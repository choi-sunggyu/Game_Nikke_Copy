using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BurstEffectTests
{
    private readonly System.Collections.Generic.List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in createdObjects)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }

        foreach (TrendHologramStageEffect effect in Object.FindObjectsByType<TrendHologramStageEffect>(FindObjectsSortMode.None))
            Object.DestroyImmediate(effect.gameObject);

        foreach (AllyBuffAuraEffect effect in Object.FindObjectsByType<AllyBuffAuraEffect>(FindObjectsSortMode.None))
            Object.DestroyImmediate(effect.gameObject);

        foreach (AstroSupernovaEffect effect in Object.FindObjectsByType<AstroSupernovaEffect>(FindObjectsSortMode.None))
            Object.DestroyImmediate(effect.gameObject);

        createdObjects.Clear();
    }

    [Test]
    public void Trend_버스트는_홀로그램_무대와_아군_버프_오라를_생성한다()
    {
        Trend trend = CreateCharacter<Trend>("Trend");
        trend.Initialize();
        TestCharacter ally = CreateCharacter<TestCharacter>("Ally");
        ally.Setup();

        int stageBefore = Object.FindObjectsByType<TrendHologramStageEffect>(FindObjectsSortMode.None).Length;
        int auraBefore = Object.FindObjectsByType<AllyBuffAuraEffect>(FindObjectsSortMode.None).Length;

        trend.UseBurst();

        int stageAfter = Object.FindObjectsByType<TrendHologramStageEffect>(FindObjectsSortMode.None).Length;
        int auraAfter = Object.FindObjectsByType<AllyBuffAuraEffect>(FindObjectsSortMode.None).Length;

        Assert.Greater(stageAfter, stageBefore);
        Assert.GreaterOrEqual(auraAfter - auraBefore, 2);
        Assert.IsTrue(trend.UsedBurstThisCycle);
    }

    [Test]
    public void Astro_버스트는_인공태양을_생성하고_틱_데미지를_준다()
    {
        Astro astro = CreateCharacter<Astro>("Astro");
        astro.Initialize();
        TestEnemy enemy = CreateEnemy("Enemy");
        float beforeHp = enemy.Hp;

        astro.UseBurst();

        AstroSupernovaEffect[] effects = Object.FindObjectsByType<AstroSupernovaEffect>(FindObjectsSortMode.None);
        Assert.Greater(effects.Length, 0);

        IEnumerator routine = (IEnumerator)InvokePrivate(
            astro,
            "SupernovaRoutine",
            new object[] { effects[0] });

        routine.MoveNext();

        Assert.Less(enemy.Hp, beforeHp);
        Assert.IsTrue(astro.UsedBurstThisCycle);
    }

    private T CreateCharacter<T>(string objectName) where T : CharacterBase
    {
        GameObject obj = new GameObject(objectName);
        createdObjects.Add(obj);
        obj.AddComponent<SpriteRenderer>();
        return obj.AddComponent<T>();
    }

    private TestEnemy CreateEnemy(string objectName)
    {
        GameObject obj = new GameObject(objectName);
        createdObjects.Add(obj);
        obj.AddComponent<SpriteRenderer>();
        TestEnemy enemy = obj.AddComponent<TestEnemy>();
        enemy.Setup();
        return enemy;
    }

    private static object InvokePrivate(object target, string methodName, object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return method.Invoke(target, args);
    }

    private class TestCharacter : CharacterBase
    {
        public void Setup()
        {
            maxHp = 100f;
            hp = maxHp;
            maxShield = 0f;
            shield = 0f;
            attackDamage = 10f;
            criticalRate = 0f;
            survive = true;
        }

        public override void Initialize() { }
        public override void UseSkill() { }
        public override void UseBurst() { }
    }

    private class TestEnemy : EnemyBase
    {
        public void Setup()
        {
            maxHp = 1000f;
            hp = maxHp;
            survive = true;
            isSpawning = false;
            SetEnemyBaseField(this, "spriteRenderer", GetComponent<SpriteRenderer>());
        }

        public override void Initialize() { }
        public override void Attack() { }
        public override void Move() { }
        public override void Jump() { }
    }

    private static void SetEnemyBaseField(EnemyBase target, string fieldName, object value)
    {
        FieldInfo field = typeof(EnemyBase).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
