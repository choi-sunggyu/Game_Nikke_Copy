using System.Collections.Generic;
using UnityEngine;

public class BurstSlotsController : MonoBehaviour
{
    [SerializeField] private List<BurstSlotUI> slots;

    private static readonly KeyCode[] SlotKeys = {
        KeyCode.A, KeyCode.S, KeyCode.D
    };

    private List<CharacterBase> currentTargets = new List<CharacterBase>();

    void OnEnable()
    {
        BurstGaugeManager.OnBurstReady    += HandleBurstReady;
        BurstGaugeManager.OnBurstConsumed += HandleBurstConsumed;
    }

    void OnDisable()
    {
        BurstGaugeManager.OnBurstReady    -= HandleBurstReady;
        BurstGaugeManager.OnBurstConsumed -= HandleBurstConsumed;
    }

    void Update()
    {
        for (int i = 0; i < currentTargets.Count && i < SlotKeys.Length; i++)
        {
            if (Input.GetKeyDown(SlotKeys[i]))
                BurstGaugeManager.Instance?.TryUseBurstByCharacter(currentTargets[i]);
        }
    }

    private void HandleBurstReady(List<CharacterBase> targets)
    {
    
        currentTargets = targets;

        foreach (var slot in slots)
        {
            slot.SlideOut();
        }

        for (int i = 0; i < targets.Count && i < slots.Count; i++)
        {
            slots[i].Setup(targets[i], i);
            slots[i].SlideIn();
        }
    }

    private void HandleBurstConsumed()
    {
        currentTargets.Clear();
        foreach (var slot in slots) slot.SlideOut();
    }
}