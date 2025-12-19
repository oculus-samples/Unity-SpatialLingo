// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    /// <summary>
    /// Unit that exits on debug skip or on a secondary input.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class LessonWaitingState : SkippableUnit
    {
        [DoNotSerialize] public ValueInput GollyGoshManagerInput;

        private TreeController m_treeController;
        private ExerciseManager m_exerciseManager;

        protected override void Definition()
        {
            base.Definition();
            GollyGoshManagerInput = ValueInput<GollyGoshInteractionManager>(nameof(GollyGoshManagerInput));
        }

        protected override void OnEnter(Flow flow)
        {
            m_treeController = Variables.Application.Get<TreeController>(nameof(TreeController));
            var gollyGoshManager = flow.GetValue<GollyGoshInteractionManager>(GollyGoshManagerInput);

            var app = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            m_exerciseManager = app.ExerciseManager;

            AppSessionData.Tier = 1;

            gollyGoshManager.ShowFaceHappy();

            m_exerciseManager.SetTreeManager(m_treeController);
            m_exerciseManager.SetGollyGoshManager(gollyGoshManager);
            m_exerciseManager.SetTargetLanguage(AppSessionData.TargetLanguage);

            // State is done with all tiers are complete and celebration is complete
            m_exerciseManager.AllTiersCompleted += OnAllTiersCompleted;
            m_exerciseManager.StartExperience();
        }

        private void OnAllTiersCompleted(ExerciseManager manager)
        {
            m_exerciseManager.AllTiersCompleted -= OnAllTiersCompleted;
            m_isDone = true;
        }

        protected override void OnExit()
        {
            m_exerciseManager.ResetForReuse();

            // Stop Lessons
            base.OnExit();
        }
    }
}