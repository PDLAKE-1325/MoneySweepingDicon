using NUnit.Framework.Internal;
using UnityEngine;

public class ClassTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TestClass t1 = new TestClass();
        TestClass t2 = new TestClass();

        t1.a = 5;
        t2.a = 10;

        print($"{t1.a} {t1.b} {t2.a} {t2.b}");

        t1 = t2;
        t1.a = 20;

        print($"{t1.a} {t1.b} {t2.a} {t2.b}");

    }
}
public class TestClass
{
    public int a;
    public int b = 10;
}