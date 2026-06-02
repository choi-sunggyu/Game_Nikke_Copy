using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;
    private int nextGhostTrigger = 400;

    public List<CharacterBase> Team;

    private int totalBulletConsumed;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        CharacterBase.OnBulletConsumed += HandleBulletConsumed;
        BulletBase.OnLastBulletHit += HandleViperSkill;
        BurstGaugeManager.OnFullBurstStarted += HandleTitanSkill;
    }

    private void OnDisable()
    {
        CharacterBase.OnBulletConsumed -= HandleBulletConsumed;
        BulletBase.OnLastBulletHit -= HandleViperSkill;
        BurstGaugeManager.OnFullBurstStarted -= HandleTitanSkill;
    }

    //GHOST
    private void HandleBulletConsumed(CharacterBase sender, int amount)
    {
        totalBulletConsumed += amount;

        if(totalBulletConsumed >= nextGhostTrigger)
        {
            ActivateGhostSkill();

            nextGhostTrigger += 400;
        }
    }

    private void ActivateGhostSkill()
    {
        Ghost ghost =
            Team.OfType<Ghost>().FirstOrDefault();

        if(ghost == null) return;

        ghost.UseSkill();
    }

    //TITAN
    private void HandleTitanSkill()
    {
        Titan titan =
            Team.OfType<Titan>()
                .FirstOrDefault();

        if (titan == null)
            return;

        titan.UseSkill();
    }

    // VIPER
    private void HandleViperSkill(CharacterBase owner)
    {
        Viper viper = owner as Viper;

        if(viper == null)
            return;

        viper.UseSkill();
    }
}