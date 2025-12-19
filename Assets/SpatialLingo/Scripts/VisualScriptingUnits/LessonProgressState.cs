// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    /// <summary>
    /// Unit that Progresses to the next lesson
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class LessonProgressState : SkippableUnit
    {
        private TreeController m_treeController;

        protected override void OnEnter(Flow flow)
        {
            m_treeController = Variables.Application.Get<TreeController>(nameof(TreeController));
            var tier = AppSessionData.Tier;
            switch (tier)
            {
                case 2:
                case 3:
                    m_treeController.AnimateToTier(tier);
                    break;

                default:
                    m_treeController.SetTier(1);
                    break;
            }
            // Auto-goto next state
            m_isDone = true;
        }
    }
}