// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.Characters;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public class SpawnSeedAndMoundUnit : Unit
    {
        [DoNotSerialize] public ControlInput Enter;
        [DoNotSerialize] public ControlOutput Exit;

        [DoNotSerialize] public ValueInput MoundPrefabInput;
        [DoNotSerialize] public ValueInput SeedPrefabInput;

        private LanguageSeedController m_languageSeedController;
        private FocusPointController m_moundController;

        protected override void Definition()
        {
            Enter = ControlInput(nameof(Enter), OnEnter);
            Exit = ControlOutput(nameof(Exit));

            MoundPrefabInput = ValueInput<FocusPointController>(nameof(MoundPrefabInput));
            SeedPrefabInput = ValueInput<LanguageSeedController>(nameof(SeedPrefabInput));
        }

        private ControlOutput OnEnter(Flow flow)
        {
            if (m_moundController != null)
            {
                Object.Destroy(m_moundController.gameObject);
            }
            var moundPrefab = flow.GetValue<FocusPointController>(MoundPrefabInput);
            m_moundController = Object.Instantiate(moundPrefab);
            Variables.Application.Set(nameof(FocusPointController), m_moundController);
            var treeController = Variables.Application.Get<TreeController>(nameof(TreeController));
            m_moundController.SetOrientation(treeController.transform.position, Quaternion.identity);
            m_moundController.ShowShimmer();

            if (m_languageSeedController != null)
            {
                Object.Destroy(m_languageSeedController.gameObject);
            }
            var seedPrefab = flow.GetValue<LanguageSeedController>(SeedPrefabInput);
            m_languageSeedController = Object.Instantiate(seedPrefab);
            Variables.Application.Set(nameof(LanguageSeedController), m_languageSeedController);
            m_languageSeedController.gameObject.SetActive(false);
            m_languageSeedController.DisableGrabInteraction();
            m_languageSeedController.MoveTo(m_moundController.transform.position, true);
            m_languageSeedController.gameObject.SetActive(true);
            m_languageSeedController.MoveTo(m_moundController.transform.position + Vector3.up, false);
            return Exit;
        }
    }
}