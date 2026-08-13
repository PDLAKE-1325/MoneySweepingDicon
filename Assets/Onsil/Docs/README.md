# 온실 (The Greenhouse) — 전투 연출 시스템

노라(HLK-04)의 스프라이트 애니메이션과 스킬 연출을 담당하는 런타임 패키지.

**대상 환경** — Unity 6000.3 / URP 17.3 / Input System 1.20 / C# 9

---

## 1. 왜 Animator를 안 쓰는가

이 시스템은 Unity의 `Animator`를 **의도적으로 사용하지 않는다.**

`Animator.Play()`는 호출할 때마다 스테이트 머신을 재평가한다. 시네마틱이 프레임을 직접 제어하려고 매 프레임 `Play(clip, 0, normalizedTime)`을 부르면 미세한 히치가 누적되어 눈에 보이는 끊김이 생긴다. 프로토타입에서 이 문제로 여러 차례 재작업했다.

대신 `SpriteAnimator`가 `SpriteRenderer.sprite`에 스프라이트를 **직접 대입**한다. 구조적으로 끊길 수 없고, 어빌리티가 임의의 셀을 임의의 시간만큼 붙잡을 수 있다.

> `SpriteAnimator.Awake()`는 같은 오브젝트의 `Animator`를 자동으로 비활성화한다.

---

## 2. 아키텍처

```
AbilityRunner ─── 캐스팅 단일 진입점, 동시 실행 차단
    │
    ├─ AbilityContext ── 어빌리티가 만질 수 있는 것 전부 (DI 컨테이너)
    │
    └─ Ability (추상)
         ├─ BasicAttackAbility     서서쏴, 마크 소비
         ├─ ScanAbility            체공 스캔, 마크 부여
         ├─ AirborneShotAbility    체공 사격, 마크 소비
         └─ UltimateAbility        앉아쏴 + 시네마틱, 마크 소비

SpriteAnimator ── 렌더러 소유, 아이들 루프
    └─ SpriteClip (ScriptableObject) ── 프레임 배열 + 명명된 구간

Vfx/
  MuzzleRig          총구화염 + 탄피
  ThrusterRig        백팩 트윈 스러스터
  LockOnReticle      지속형 락온 마크
  ImpactJet          성형작약 제트
  CinematicDirector  컷어웨이 무대 + 레터박스 + 슬로우
  BadAppleController 흑백 임계값 셰이더 제어
Camera/
  CameraShaker       가산형 흔들림
```

**의존 방향은 한쪽이다.** 어빌리티는 VFX를 호출하지만, VFX는 어빌리티를 모른다.

---

## 3. 빠른 시작

### 3-1. 캐릭터 셋업

```
Nora (GameObject)
├── SpriteRenderer          Sprite (2D and UI), PPU 256, Point, Compression None
├── SpriteAnimator          body = 자기 SpriteRenderer, idleClip = nora_idle
├── AbilityRunner           target / battleCamera / reticle / director / rigs 연결
├── MuzzleRig
├── ThrusterRig
├── BasicAttackAbility      abilityId = "basic"
├── ScanAbility             abilityId = "scan"
├── AirborneShotAbility     abilityId = "airshot"
└── UltimateAbility         abilityId = "ultimate"
```

### 3-2. 캐스팅

```csharp
var runner = nora.GetComponent<AbilityRunner>();

if (runner.Cast("ultimate"))
    Debug.Log("수락됨");     // 이미 실행 중이거나 CanCast() 실패면 false
```

### 3-3. 이벤트 구독

```csharp
runner.AbilityStarted  += id => hud.Lock();
runner.AbilityFinished += id => hud.Unlock();

reticle.MarkApplied  += t => combat.ApplyVulnerable(t);
reticle.MarkConsumed += t => combat.DealBonusDamage(t);
```

---

## 4. 어빌리티 참조 구현

아래 네 개는 프로토타입에서 검증을 마친 타이밍이다. 그대로 붙여넣어 동작시키고,
숫자만 조정하면 된다. 모두 `Assets/Onsil/Runtime/Abilities/`에 두면 된다.

### 공통 규약

| 스킬 | `abilityId` | 마크 | 클립 |
|---|---|---|---|
| 평타 | `"basic"` | 소비 | `nora_fire` |
| 스킬1 스캔 | `"scan"` | **부여** | `nora_jumpshot` |
| 스킬2 체공사격 | `"airshot"` | 소비 | `nora_jumpshot` |
| 궁극기 | `"ultimate"` | 소비 | `nora_kneel` |

마크 처리는 `AbilityRunner`가 플래그를 보고 자동으로 한다. `Run()` 안에서
`reticle.Consume()`을 직접 부르지 말 것 — 두 번 소비된다.



### 4-1. 평타 — 서서 조준사격

```csharp
using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>Standing aimed shot. Spends the lock-on mark if one is present.</summary>
    public class BasicAttackAbility : Ability
    {
        [SerializeField] SpriteClip clip;                 // nora_fire, 13 cells
        [SerializeField] float aimHold  = 0.32f;          // settle before the trigger
        [SerializeField] float firePause = 0.08f;         // hold ON the fire cell
        [SerializeField] float clipFps  = 14f;

        void Reset() { abilityId = "basic"; consumesMark = true; appliesMark = false; }

        public override IEnumerator Run()
        {
            var anim = Ctx.Animator;

            // cells 0-5 : raise and settle on target
            yield return anim.Play(clip, "aim", clipFps);
            yield return anim.Hold(clip, clip.RangeOrAll("aim").last, aimHold);

            // cell 6 : THE SHOT
            var fire = clip.RangeOrAll("fire");
            anim.Show(clip, fire.first);
            Ctx.Muzzle?.Fire(MuzzleRig.Stance.Standing);
            Ctx.Shaker?.Shake(0.16f);
            yield return anim.Hold(clip, fire.first, firePause);

            // cells 7-12 : recoil and recover
            yield return anim.Play(clip, "recover", clipFps);
        }
    }
}
```

---

### 4-2. 스킬 1 — 체공 스캔 (마크 부여)

유일하게 마크를 **거는** 스킬. 총을 쏘지 않으므로 격발 셀에 절대 닿으면 안 된다.

```csharp
using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>Jetpack up, sweep the target, leave a lock-on mark behind.</summary>
    public class ScanAbility : Ability
    {
        [SerializeField] SpriteClip clip;                 // nora_jumpshot, 36 cells
        [SerializeField] Sprite scanLine;

        [Header("flight")]
        [SerializeField] float jumpHeight = 2.4f;
        [SerializeField] float jumpBack   = 0f;           // keep 0: the art is a vertical climb
        [SerializeField, Range(0f, 0.8f)] float liftDelay = 0.45f;
        [SerializeField] float riseSharpness = 9f;
        [SerializeField] float airLean = 12f;
        [SerializeField] float clipFps = 18f;

        [Header("scan")]
        [SerializeField] float sweepTime = 0.9f;
        [SerializeField] int   sweepPasses = 2;

        /// Logarithmic rise: hard burst off the ground, easing into the hover.
        static float LogRise(float k, float sharp)
        {
            sharp = Mathf.Max(sharp, 0.01f);
            return Mathf.Log(1f + sharp * Mathf.Clamp01(k)) / Mathf.Log(1f + sharp);
        }

        void Reset() { abilityId = "scan"; consumesMark = false; appliesMark = true; }

        public override IEnumerator Run()
        {
            var anim = Ctx.Animator;
            var th   = Ctx.Thruster;
            Vector3 home = Ctx.Self.position;
            Vector3 apex = home + new Vector3(-jumpBack, jumpHeight, 0f);

            // cells 0-4 : compress. Nothing moves, nothing burns.
            yield return anim.Play(clip, "crouch", clipFps + 4f, _ => th?.SetPower(0f));

            // cells 5-13 : rise. liftDelay holds the ground for the first 45%,
            // because cells 5-7 are still a deep crouch (measured: top_z flat).
            // Body and plume start together - never one before the other.
            yield return anim.Play(clip, "rise", clipFps + 4f, k =>
            {
                float kk = Mathf.InverseLerp(liftDelay, 1f, k);
                Ctx.Self.position = Vector3.Lerp(home, apex, LogRise(kk, riseSharpness));
                th?.SetPower(kk <= 0f ? 0f : Mathf.Min(1f, kk * 4f));
                Lean(kk);
            });
            Ctx.Self.position = apex;
            Lean(1f);

            // cells 14-16 : settle on target. STOP before the fire cell.
            var aim  = clip.RangeOrAll("aim");
            var fire = clip.RangeOrAll("fire");
            yield return anim.Play(clip, aim.first, fire.first - 1, clipFps + 2f,
                                   _ => th?.SetPower(0.45f));

            yield return SweepBeam(anim, th, fire.first - 1);
            // the mark itself is applied by AbilityRunner via appliesMark

            // cells 23-28 then 29-35 : descend and land
            yield return anim.Play(clip, "descend", clipFps + 4f, k =>
            {
                Ctx.Self.position = Vector3.Lerp(apex, home, k * k);
                th?.SetPower(0.25f * (1f - k));
                Lean(1f - k);
            });
            Ctx.Self.position = home;
            Lean(0f);
            th?.Off();
            Ctx.Shaker?.Shake(0.16f);
            yield return anim.Play(clip, "land", clipFps + 4f);
            Ctx.Self.position = home;
        }

        void Lean(float amount)
        {
            var body = Ctx.Animator.Body;
            if (body != null)
                body.transform.localRotation = Quaternion.Euler(0f, 0f, -airLean * amount);
        }

        /// Sight line + a band sweeping the target + brackets closing in.
        IEnumerator SweepBeam(SpriteAnimator anim, ThrusterRig th, int holdCell)
        {
            if (Ctx.Target == null) yield break;

            Vector3 origin = Ctx.Self.position +
                             (Vector3)(Ctx.Muzzle != null
                                 ? Ctx.Muzzle.OffsetFor(MuzzleRig.Stance.Airborne)
                                 : Vector2.zero);
            Vector3 focus  = Ctx.Target.position + Vector3.up * 0.45f;

            var beam = NewSprite("ScanBeam", scanLine, 40);
            Vector3 d = focus - origin;
            beam.transform.position = origin + d * 0.5f;
            beam.transform.rotation =
                Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            beam.transform.localScale = new Vector3(d.magnitude * 1.9f, 0.05f, 1f);

            var band = NewSprite("ScanBand", scanLine, 41);

            float t = 0f;
            while (t < sweepTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / sweepTime);
                float fade = Mathf.Clamp01(k / 0.12f) * Mathf.Clamp01((1f - k) / 0.15f);

                SetAlpha(beam, 0.75f * fade);

                float p = Mathf.PingPong(k * sweepPasses, 1f);
                band.transform.position =
                    new Vector3(focus.x, Mathf.Lerp(focus.y + 0.6f, focus.y - 0.6f, p), -0.25f);
                band.transform.localScale = new Vector3(1.6f, 0.05f, 1f);
                SetAlpha(band, 0.95f * fade);

                anim.Show(clip, holdCell);      // stay on the aim pose
                th?.SetPower(0.45f);
                yield return null;
            }
            Object.Destroy(beam);
            Object.Destroy(band);
        }

        static GameObject NewSprite(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = new Color(0.45f, 0.95f, 1f, 0f);
            return go;
        }

        static void SetAlpha(GameObject go, float a)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            var c = sr.color; c.a = a; sr.color = c;
        }
    }
}
```

---

### 4-3. 스킬 2 — 체공 사격 (마크 소비)

스캔과 상승 로직은 같고, 격발 셀에서 실제로 쏜다.

```csharp
using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>Jetpack up and fire. Spends the mark for bonus damage.</summary>
    public class AirborneShotAbility : Ability
    {
        [SerializeField] SpriteClip clip;                 // nora_jumpshot

        [Header("flight")]
        [SerializeField] float jumpHeight = 2.4f;
        [SerializeField] float jumpBack   = 0f;
        [SerializeField, Range(0f, 0.8f)] float liftDelay = 0.45f;
        [SerializeField] float riseSharpness = 9f;
        [SerializeField] float airLean = 12f;
        [SerializeField] float clipFps = 18f;

        [Header("timing")]
        [SerializeField] float hoverTime = 0.10f;
        [SerializeField] float aimHold   = 0.28f;
        [SerializeField] float firePause = 0.12f;

        static float LogRise(float k, float sharp)
        {
            sharp = Mathf.Max(sharp, 0.01f);
            return Mathf.Log(1f + sharp * Mathf.Clamp01(k)) / Mathf.Log(1f + sharp);
        }

        void Reset() { abilityId = "airshot"; consumesMark = true; appliesMark = false; }

        public override IEnumerator Run()
        {
            var anim = Ctx.Animator;
            var th   = Ctx.Thruster;
            Vector3 home = Ctx.Self.position;
            Vector3 apex = home + new Vector3(-jumpBack, jumpHeight, 0f);

            yield return anim.Play(clip, "crouch", clipFps + 4f, _ => th?.SetPower(0f));

            yield return anim.Play(clip, "rise", clipFps + 4f, k =>
            {
                float kk = Mathf.InverseLerp(liftDelay, 1f, k);
                Ctx.Self.position = Vector3.Lerp(home, apex, LogRise(kk, riseSharpness));
                th?.SetPower(kk <= 0f ? 0f : Mathf.Min(1f, kk * 4f));
                Lean(kk);
            });
            Ctx.Self.position = apex;
            Lean(1f);

            var aim  = clip.RangeOrAll("aim");
            var fire = clip.RangeOrAll("fire");     // cell 17

            yield return anim.Hold(clip, aim.first, hoverTime, _ => th?.SetPower(0.45f));
            yield return anim.Play(clip, aim.first, fire.first - 1, clipFps + 2f,
                                   _ => th?.SetPower(0.45f));
            yield return anim.Hold(clip, fire.first - 1, aimHold, _ => th?.SetPower(0.45f));

            // cell 17 : THE SHOT. Firing on the aim cell instead looks desynced.
            anim.Show(clip, fire.first);
            Ctx.Muzzle?.Fire(MuzzleRig.Stance.Airborne);
            Ctx.Shaker?.Shake(0.18f);
            yield return anim.Hold(clip, fire.first, firePause, _ => th?.SetPower(0.5f));

            yield return anim.Play(clip, "recoil", clipFps + 4f, _ => th?.SetPower(0.45f));

            yield return anim.Play(clip, "descend", clipFps + 4f, k =>
            {
                Ctx.Self.position = Vector3.Lerp(apex, home, k * k);
                th?.SetPower(0.25f * (1f - k));
                Lean(1f - k);
            });
            Ctx.Self.position = home;
            Lean(0f);
            th?.Off();
            Ctx.Shaker?.Shake(0.16f);
            yield return anim.Play(clip, "land", clipFps + 4f);
            Ctx.Self.position = home;
        }

        void Lean(float amount)
        {
            var body = Ctx.Animator.Body;
            if (body != null)
                body.transform.localRotation = Quaternion.Euler(0f, 0f, -airLean * amount);
        }
    }
}
```

---

### 4-4. 궁극기 — 앉아쏴 + 시네마틱

유일하게 `CinematicDirector`를 쓰는 스킬.

```csharp
using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>
    /// Drop to a knee, fire, cut to the round's flight, then a black-and-white
    /// slow-motion impact.
    /// </summary>
    public class UltimateAbility : Ability
    {
        [SerializeField] SpriteClip clip;                 // nora_kneel, 13 cells
        [SerializeField] ImpactJet  jet;
        [SerializeField] float aimHold   = 0.5f;
        [SerializeField] float firePause = 0.05f;
        [SerializeField] float clipFps   = 12f;

        void Reset() { abilityId = "ultimate"; consumesMark = true; appliesMark = false; }

        public override IEnumerator Run()
        {
            var anim = Ctx.Animator;
            var dir  = Ctx.Director;

            // cells 0-1 drop, 2-5 settle. Hold on cell 5, NOT cell 6 -
            // cell 6 is the fire pose and holding it looks like she already shot.
            yield return anim.Play(clip, "drop",   clipFps);
            yield return anim.Play(clip, "settle", clipFps);
            yield return anim.Hold(clip, clip.RangeOrAll("settle").last, aimHold);

            // cell 6 : THE SHOT
            var fire = clip.RangeOrAll("fire");
            anim.Show(clip, fire.first);
            Ctx.Muzzle?.Fire(MuzzleRig.Stance.Kneeling);
            Ctx.Shaker?.Shake(0.18f);
            yield return anim.Hold(clip, fire.first, firePause);

            // ---- cut away ----
            dir.Build(Ctx.BattleCamera);
            yield return dir.RunRound();

            BadAppleController.Instance?.FadeIn(0.04f);
            dir.CutCamera.GetComponent<CameraShaker>()?.Shake(0.4f, 0.43f);
            jet?.Spawn(dir.ImpactPoint, dir.CutCamera.transform.parent,
                       dir.cutawayLayer, dir.CutCamera);

            yield return dir.RunSlowMotion();          // sets and restores timeScale

            BadAppleController.Instance?.FadeOut(0.4f);
            dir.Teardown();
            yield return new WaitForSeconds(dir.returnPause);
            dir.SetLetterbox(false);
            BadAppleController.Instance?.SetAmount(0f); // safety: never leave it stuck

            // cells 7-12 in ONE continuous run. A hold in the middle reads as a freeze.
            var rec = clip.RangeOrAll("recoil");
            var end = clip.RangeOrAll("recover");
            yield return anim.Play(clip, rec.first, end.last, clipFps);
        }
    }
}
```

---

## 5. 새 어빌리티 추가

클래스 하나만 만들면 된다.

```csharp
using System.Collections;
using UnityEngine;
using Onsil.Abilities;
using Onsil.Vfx;

public class GrenadeAbility : Ability
{
    [SerializeField] Onsil.Actors.SpriteClip clip;
    [SerializeField] float throwDelay = 0.3f;

    void Reset()
    {
        abilityId   = "grenade";
        consumesMark = false;      // 마크를 안 쓰는 스킬
    }

    public override bool CanCast() => !onCooldown;

    public override IEnumerator Run()
    {
        var anim = Ctx.Animator;

        yield return anim.Play(clip, "windup");
        yield return anim.Hold(clip, clip.RangeOrAll("windup").last, throwDelay);

        Ctx.Muzzle.Flash(MuzzleRig.Stance.Standing);
        Ctx.Shaker.Shake(0.15f);

        yield return anim.Play(clip, "release");
        yield return anim.Play(clip, "recover");
    }
}
```

컴포넌트를 캐릭터에 붙이면 `AbilityRunner`가 `Awake`에서 자동 등록한다. `abilityId`가 비었거나 중복이면 경고를 찍고 건너뛴다.

---

## 6. SpriteClip

`Assets > Create > Onsil > Sprite Clip`

| 필드 | 설명 |
|---|---|
| `frames` | 재생 순서대로 정렬된 스프라이트 배열 |
| `fps` | 기본 재생 속도. 어빌리티가 구간별로 덮어쓸 수 있음 |
| `loop` | 아이들 계열만 true |
| `ranges` | 명명된 셀 구간 `{ id, first, last }` |

**셀 인덱스는 시트 내 인덱스다.** Blender 소스 프레임 번호가 아니다.

### 현재 클립 구조

**nora_kneel** (13셀 / 14fps)
```
drop     0-1     무릎 꿇기
settle   2-5     자세·장전
fire     6       ★ 격발
recoil   7-9     반동
recover 10-12    일어서기
```

**nora_jumpshot** (36셀 / 16fps)
```
crouch    0-4    src 0-8     서기→웅크림
rise      5-13   src 9-18    도약 + 상승
aim      14-16   src 20-26   조준
fire     17      src 27      ★ 격발 (26→28 반동 스냅)
recoil   18-22   src 28-37
descend  23-28   src 41-62
land     29-35   src 63-72
```

> **주의** — 조준 구간을 셀 22까지 잡으면 격발 프레임이 포함되어 스캔 중에도 사격 자세가 나온다. 반드시 `fire - 1`에서 끊을 것.

---

## 7. 스프라이트 규격

```
셀          288 × 256 px
캐릭터 높이  224 px (모든 모션 공통)
피벗        Custom, 각 모션 0번 셀의 발 중심
PPU         256
필터        Point (no filter)
압축        None
Max Size    16384   ← 점프샷 시트가 10368px
```

### 피벗이 Custom인 이유

모션마다 셀 안에서 캐릭터가 그려진 x 위치가 다르다. 실측값:

```
idle      138.5      fire      103.9
parry     137.1      kneel     107.9
jumpshot  119.9
```

Bottom Center(=144)로 고정하면 모션 전환 시 최대 **34.6px** 순간이동한다. 각 모션 **0번 셀의 발 중심**을 피벗으로 잡아 이를 제거했다.

프레임마다 재계산하면 안 된다 — 앉아쏴에서 앞발이 나가는 66px 연기까지 상쇄되어 캐릭터가 미끄러진다.

---

## 8. 흑백 셰이더

`Onsil/BadAppleThreshold` — 휘도 임계값 이진화.

```csharp
BadAppleController.Instance.FadeIn(0.05f);   // 흑백 진입
BadAppleController.Instance.FadeOut(0.4f);   // 복귀
BadAppleController.Instance.SetAmount(0f);   // 즉시 리셋 (안전장치)
BadAppleController.Instance.Hit(0.4f, 0.5f); // 짧게 번쩍
```

| 프로퍼티 | 역할 |
|---|---|
| `_Amount` | 0이면 **완전 패스스루**. 탈색도 이 값에 스케일됨 |
| `_Threshold` | 흑/백 분기점 |
| `_Softness` | 경계 부드러움 |
| `_Invert` | 반전 |
| `_Bright` / `_Dark` | 출력 2색 |

### 설치 요건 (둘 다 필요)

1. `Assets/Settings/NoraRenderer.asset`의 **Full Screen Pass Renderer Feature**에 `BadApple.mat` 연결, Injection Point = `AfterRenderingPostProcessing`
2. 카메라의 **`renderPostProcessing = true`**

> 2번이 꺼져 있으면 주입 단계 자체가 실행되지 않아 패스가 통째로 스킵된다. 셰이더가 멀쩡한데 아무것도 안 나오면 여기부터 확인할 것.

> URP 17에는 `Blit.hlsl`이 없다. Core RP의 `GetFullScreenTriangleVertexPosition()`으로 직접 풀스크린 삼각형을 만든다.

### 컨트롤러 참조 구현

`Assets/Onsil/Runtime/Vfx/BadAppleController.cs`로 저장한다. 씬 어딘가에 하나만 두면 된다.

```csharp
using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Drives the black-and-white threshold pass.
    /// </summary>
    /// <remarks>
    /// Uses ONE dedicated coroutine handle. An earlier version called
    /// StopAllCoroutines inside FadeOut, so an unrelated coroutine on the same
    /// object could cancel the fade and leave the screen stuck in monochrome.
    /// </remarks>
    [ExecuteAlways]
    public class BadAppleController : MonoBehaviour
    {
        public static BadAppleController Instance { get; private set; }

        [Tooltip("Material using Onsil/BadAppleThreshold")]
        public Material material;

        [Range(0, 1)] public float amount = 0f;
        [Range(0, 1)] public float threshold = 0.5f;
        [Range(0.001f, 0.4f)] public float softness = 0.05f;
        [Range(0, 1)] public float invert = 0f;
        [Range(0, 1)] public float desaturate = 1f;
        public Color bright = Color.white;
        public Color dark = Color.black;

        static readonly int ID_Amount    = Shader.PropertyToID("_Amount");
        static readonly int ID_Threshold = Shader.PropertyToID("_Threshold");
        static readonly int ID_Softness  = Shader.PropertyToID("_Softness");
        static readonly int ID_Invert    = Shader.PropertyToID("_Invert");
        static readonly int ID_Desat     = Shader.PropertyToID("_Desat");
        static readonly int ID_Bright    = Shader.PropertyToID("_Bright");
        static readonly int ID_Dark      = Shader.PropertyToID("_Dark");

        Coroutine anim;

        void OnEnable()  { Instance = this; Push(); }
        void OnDisable() { amount = 0f; Push(); }   // never leave the screen stuck
        void OnValidate() => Push();
        void LateUpdate() => Push();

        public void Push()
        {
            if (material == null) return;
            material.SetFloat(ID_Amount, amount);
            material.SetFloat(ID_Threshold, threshold);
            material.SetFloat(ID_Softness, softness);
            material.SetFloat(ID_Invert, invert);
            material.SetFloat(ID_Desat, desaturate);
            material.SetColor(ID_Bright, bright);
            material.SetColor(ID_Dark, dark);
        }

        void Run(IEnumerator routine)
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(routine);
        }

        /// <summary>Jump straight to a value, cancelling any fade in flight.</summary>
        public void SetAmount(float v)
        {
            if (anim != null) { StopCoroutine(anim); anim = null; }
            amount = Mathf.Clamp01(v);
            Push();
        }

        public void FadeIn(float dur = 0.25f)  => Run(Ramp(amount, 1f, dur));
        public void FadeOut(float dur = 0.4f)  => Run(Ramp(amount, 0f, dur));
        public void Hit(float hold = 0.35f, float release = 0.5f) => Run(HitRoutine(hold, release));

        IEnumerator Ramp(float a, float b, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;        // must survive slow motion
                amount = Mathf.Lerp(a, b, Mathf.Clamp01(t / dur));
                Push();
                yield return null;
            }
            amount = b; Push(); anim = null;
        }

        IEnumerator HitRoutine(float hold, float release)
        {
            amount = 1f; Push();
            yield return new WaitForSecondsRealtime(hold);
            float t = 0f;
            while (t < release)
            {
                t += Time.unscaledDeltaTime;
                amount = 1f - Mathf.Clamp01(t / release);
                Push();
                yield return null;
            }
            amount = 0f; Push(); anim = null;
        }
    }
}
```


---

## 9. VFX 스프라이트 제작 규칙

### 알파 블리딩은 필수

투명 픽셀의 RGB가 검정(0,0,0)이면 bilinear 필터가 경계에서 그것을 섞어 **검은 테두리**가 생긴다. 실제로 `vfx_gas`에 11,358개, `vfx_ultfan`에 128,172개가 있었다.

새 VFX 스프라이트를 추가하면 반드시 `Tools > Onsil > Bleed Alpha`를 실행할 것.

### 피벗 규약

| 스프라이트 | 피벗 | 이유 |
|---|---|---|
| `vfx_thruster` | TopCenter | 노즐에서 아래로 늘어짐 |
| `vfx_ultfan` | LeftCenter | 타격점에서 앞으로 자람 |
| `vfx_gas` | LeftCenter | 총구 앞으로 부품 |
| 그 외 | Center | |

중앙 피벗 스프라이트를 방향성 있게 쓰려면 **피벗 오브젝트를 앵커에 두고 자식을 길이의 절반만큼 밀어야** 한다. 스케일만 키우면 절반이 뒤로 삐져나온다.

---

## 10. 타이밍 파라미터

### CinematicDirector

| 필드 | 기본 | 설명 |
|---|---|---|
| `flyTime` | 0.8 | 탄자 비행 시간 |
| `tracerReach` | 1.0 | 1 = 표적에 정확히 도달, 1.2 = 관통 |
| `impactOffset` | 0 | 음수 = 조기 폭발, 양수 = 지연 |
| `targetStopX` | −0.75 | 표적 정지 위치 (화면 좌측 1/3) |
| `approachCurve` | 2.2 | >1이면 늦게까지 멀리 있다 훅 다가옴 |
| `slowScale` | 0.12 | 슬로우모션 배율 |
| `slowSeconds` | 2.0 | 슬로우 지속 (언스케일드) |

### ImpactJet

| 필드 | 기본 | 설명 |
|---|---|---|
| `volume` | 3.5 | 전체 크기 |
| `length` | 1.4 | 길이/폭 비율 |
| `bulgePosition` | 0.5 | 팽창 위치. 0=타격점, 1=끝 |
| `spread` | 26° | 파편 원뿔 반각 |
| `life` / `hold` | 1.9 / 0.7 | 지속시간 / 불투명 유지 비율 |

> `ImpactJet`은 **스케일드 시간**을 쓴다. 슬로우모션 동안 늘어져 재생되어야 하기 때문. 언스케일드로 바꾸면 슬로우가 시작되기도 전에 끝난다.

### 체공 (ScanAbility / AirborneShotAbility)

| 필드 | 기본 | 설명 |
|---|---|---|
| `jumpHeight` | 2.4 | 상승 고도 |
| `jumpBack` | 0 | **0 유지** — 애니메이션이 수직 상승 전제 |
| `liftDelay` | 0.45 | 상승 구간 중 지면에 붙어 있는 비율 |
| `riseSharpness` | 9 | 로그 곡선 급격함 |
| `airLean` | 12° | 적 쪽 기울기 |

`liftDelay`가 **몸의 상승과 스러스터 점화를 동시에** 제어한다. 셀 5~7이 아직 웅크린 자세(Blender 실측: top_z가 1.011로 평평)이므로 그 구간엔 아무 일도 일어나면 안 된다.

---

## 11. 알려진 함정

| 증상 | 원인 | 대처 |
|---|---|---|
| 화면이 계속 흑백 | `_Amount`와 무관하게 탈색이 적용됨 | 셰이더에서 `_Desat * _Amount`로 스케일 |
| 흑백이 안 풀림 | `FadeOut` 코루틴이 `StopAllCoroutines`에 잘림 | 전용 핸들 사용 (`BadAppleController`가 처리) |
| 셰이더가 아무 반응 없음 | 카메라 post-processing 꺼짐 | `renderPostProcessing = true` |
| 스프라이트 흐림 | Max Size 2048 | 16384로 상향 |
| 모션 전환 시 캐릭터 점프 | 피벗이 Bottom Center | Custom 피벗 사용 |
| VFX에 검은 테두리 | 투명 픽셀 RGB = 검정 | 알파 블리딩 |
| 애니메이션 끊김 | `Animator.Play()` 매 프레임 호출 | `SpriteAnimator` 사용 |
| 폭발이 슬로우 중 사라짐 | 언스케일드 시간 + pow 페이드 | 스케일드 시간 + hold 구간 |
| Play 중 씬 수정이 사라짐 | 에디터 특성 | Play 종료 후 배선 |

---

## 12. 에디터 도구

`Tools > Onsil >`

| 메뉴 | 기능 |
|---|---|
| Slice Sheets | 288×256 그리드 슬라이스 + Custom 피벗 + 임포트 설정 |
| Measure Muzzle | 총 레이어에서 총구 좌표 실측 |
| Bleed Alpha | VFX 스프라이트 알파 블리딩 |
| Build Clips | 슬라이스된 시트에서 `SpriteClip` 생성 |

---

## 13. 이식 체크리스트

- [ ] `Assets/Onsil/` 폴더 전체 복사
- [ ] URP Renderer에 Full Screen Pass Feature 추가, `BadApple.mat` 연결
- [ ] 레이어 31을 `UltCutaway`로 명명
- [ ] 배틀 카메라의 culling mask에서 레이어 31 **제외**
- [ ] 배틀 카메라 `renderPostProcessing = true`
- [ ] 캐릭터에 컴포넌트 배치 (3-1 참조)
- [ ] `SpriteClip` 에셋 생성 후 구간 지정
- [ ] `Cast(abilityId)`를 입력/턴 시스템에 연결
