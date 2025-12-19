// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public class GollyGoshSetupUnit : SkippableUnit
    {
        [DoNotSerialize] public ValueInput GollyGoshManagerInput;

        private GollyGoshInteractionManager m_ggManager;

        protected override void Definition()
        {
            base.Definition();
            GollyGoshManagerInput = ValueInput<GollyGoshInteractionManager>(nameof(GollyGoshManagerInput));
        }

        protected override void OnEnter(Flow flow)
        {
            var app = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            m_ggManager = flow.GetValue<GollyGoshInteractionManager>(GollyGoshManagerInput);
            m_ggManager.GollyGoshSpawned += OnGollyGoshSpawned;
            m_ggManager.Initialize(app.LessonInteractionManager, app.HeadsetEyeCenterTransform, app.Speaker, app.RoomSense);
        }

        private void OnGollyGoshSpawned()
        {
            // AUTO-EXIT
            m_isDone = true;
        }

        protected override void OnExit()
        {
            m_ggManager.GollyGoshSpawned -= OnGollyGoshSpawned;
        }
    }
}