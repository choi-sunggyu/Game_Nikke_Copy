using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CombatDamageAndSpawnTests
{
    private GameObject characterObject;
    private TestCharacter character;
    private GameObject enemyObject;
    private TestEnemy enemy;

    [SetUp]
    public void SetUp()
    {
        characterObject = new GameObject("TestCharacter");
        characterObject.AddComponent<SpriteRenderer>();
        character = characterObject.AddComponent<TestCharacter>();
        character.Setup(maxHp: 100f);

        enemyObject = new GameObject("TestEnemy");
        enemyObject.AddComponent<SpriteRenderer>();
        enemy = enemyObject.AddComponent<TestEnemy>();
        enemy.Setup(maxHp: 100f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(characterObject);
        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void 캐릭터_공격에_적_HP가_깎인다()
    {
        GameObject bulletObject = new GameObject("PlayerBullet");
        bulletObject.AddComponent<PoolObject>();
        BulletBase bullet = bulletObject.AddComponent<BulletBase>();
        BoxCollider collider = enemyObject.AddComponent<BoxCollider>();
        float beforeHp = enemy.Hp;

        bullet.Init(character, 25f, 0f, Vector3.right, 0f);
        InvokePrivate(
            bullet,
            "HandleCollision",
            new object[] { collider });

        Assert.AreEqual(beforeHp - 25f, enemy.Hp);
        Object.DestroyImmediate(bulletObject);
    }

    [Test]
    public void 적_일반총_공격에_캐릭터_HP가_깎인다()
    {
        GameObject bulletObject = new GameObject("EnemyBullet");
        bulletObject.AddComponent<PoolObject>();
        EnemyBulletBase bullet = bulletObject.AddComponent<EnemyBulletBase>();
        CircleCollider2D collider = characterObject.AddComponent<CircleCollider2D>();
        float beforeHp = character.CurrentHp;

        bullet.Init(15f, 0f, Vector3.right);
        InvokePrivate(
            bullet,
            "OnTriggerEnter2D",
            new object[] { collider });

        Assert.AreEqual(beforeHp - 15f, character.CurrentHp);
        Object.DestroyImmediate(bulletObject);
    }

    [Test]
    public void 적_레이저_공격에_캐릭터_HP가_깎인다()
    {
        GameObject laserEnemyObject = new GameObject("LaserEnemy");
        laserEnemyObject.AddComponent<SpriteRenderer>();
        EnemyA laserEnemy = laserEnemyObject.AddComponent<EnemyA>();
        Transform muzzle = new GameObject("LaserMuzzle").transform;
        muzzle.SetParent(laserEnemyObject.transform);

        SetEnemyBaseField(laserEnemy, "survive", true);
        SetEnemyBaseField(laserEnemy, "isSpawning", false);
        SetEnemyBaseField(laserEnemy, "attackDamage", 30f);
        SetEnemyBaseField(laserEnemy, "muzzlePoint", muzzle);
        SetEnemyBaseField(laserEnemy, "targetStrategy", new FixedTargetStrategy(character));
        SetPrivateField(laserEnemy, "warningDuration", 0f);
        SetPrivateField(laserEnemy, "laserDuration", 0f);
        SetPrivateField(laserEnemy, "warningCircle", CreateLineRenderer("WarningCircle", laserEnemyObject.transform));
        SetPrivateField(laserEnemy, "laserLine", CreateLineRenderer("LaserLine", laserEnemyObject.transform));

        float beforeHp = character.CurrentHp;

        var routine = (System.Collections.IEnumerator)InvokePrivate(
            laserEnemy,
            "LaserAttackRoutine",
            new object[0]);
        while (routine.MoveNext()) { }

        Assert.AreEqual(beforeHp - 30f, character.CurrentHp);
        Object.DestroyImmediate(laserEnemyObject);
    }

    [Test]
    public void 적_미사일_공격에_캐릭터_HP가_깎인다()
    {
        GameObject missileObject = new GameObject("EnemyMissile");
        missileObject.AddComponent<PoolObject>();
        EnemyMissileBase missile = missileObject.AddComponent<EnemyMissileBase>();
        BoxCollider collider = characterObject.AddComponent<BoxCollider>();
        float beforeHp = character.CurrentHp;

        missile.Init(40f, 0f, character.transform, 0f, 10f);
        InvokePrivate(
            missile,
            "OnTriggerEnter",
            new object[] { collider });

        Assert.AreEqual(beforeHp - 40f, character.CurrentHp);
        Object.DestroyImmediate(missileObject);
    }

    [Test]
    public void 스폰_중인_적은_공격하지_않는다()
    {
        enemy.SetSpawning(true);

        enemy.TryAttack();

        Assert.AreEqual(0, enemy.AttackCount);
    }

    [Test]
    public void 스폰이_끝난_적만_공격할_수_있다()
    {
        enemy.SetSpawning(false);

        enemy.TryAttack();

        Assert.AreEqual(1, enemy.AttackCount);
    }

    private class TestEnemy : EnemyBase
    {
        public int AttackCount { get; private set; }

        public void Setup(float maxHp)
        {
            this.maxHp = maxHp;
            hp = maxHp;
            survive = true;
            attackDamage = 10f;
            isSpawning = false;

            SetEnemyBaseField(this, "spriteRenderer", GetComponent<SpriteRenderer>());
        }

        public void SetSpawning(bool value)
        {
            isSpawning = value;
        }

        public override void Initialize() { }

        public override void Attack()
        {
            AttackCount++;
        }

        public override void Move() { }
        public override void Jump() { }
    }

    private class TestCharacter : CharacterBase
    {
        public float CurrentHp => hp;

        public void Setup(float maxHp)
        {
            this.maxHp = maxHp;
            hp = maxHp;
            maxShield = 0f;
            shield = 0f;
            survive = true;
            ChangeState(CharacterState.Fire);
        }

        public override void Initialize() { }
        public override void UseSkill() { }
        public override void UseBurst() { }
    }

    private class FixedTargetStrategy : ITargetStrategy
    {
        private readonly ITargetable target;

        public FixedTargetStrategy(ITargetable target)
        {
            this.target = target;
        }

        public ITargetable GetTarget()
        {
            return target;
        }
    }

    private static LineRenderer CreateLineRenderer(string name, Transform parent)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        return lineRenderer;
    }

    private static object InvokePrivate(object target, string methodName, object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return method.Invoke(target, args);
    }

    private static void SetEnemyBaseField(EnemyBase target, string fieldName, object value)
    {
        FieldInfo field = typeof(EnemyBase).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
