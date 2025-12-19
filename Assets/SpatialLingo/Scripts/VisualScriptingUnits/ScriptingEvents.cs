// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public static class ScriptEventNames
    {
        public const string DEBUG_SKIP = nameof(DEBUG_SKIP);
        public const string CAMERA_PERMISSION_ACCEPTED = nameof(CAMERA_PERMISSION_ACCEPTED);
        public const string USER_CONSENT = nameof(USER_CONSENT);
        public const string LANGUAGE_SELECTED = nameof(LANGUAGE_SELECTED);
        public const string LESSONS_SPAWNED = nameof(LESSONS_SPAWNED);
        public const string START_USER_LESSON_ATTEMPT = nameof(START_USER_LESSON_ATTEMPT);
        public const string USER_ATTEMPTED_LESSON = nameof(USER_ATTEMPTED_LESSON);
        public const string LESSON_TIER_COMPLETE = nameof(LESSON_TIER_COMPLETE);
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On Debug Skip")]
    public class SkipEvent : EventUnitWrapper
    {
        protected override string EventName => ScriptEventNames.DEBUG_SKIP;
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On Camera Permission Accepted")]
    public class CameraPermissionEvent : EventUnitWrapper
    {
        protected override string EventName => ScriptEventNames.CAMERA_PERMISSION_ACCEPTED;
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On User Consent Accepted")]
    public class UserConsentEvent : EventUnitWrapper
    {
        protected override string EventName => ScriptEventNames.USER_CONSENT;
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On Language Selected")]
    public class LanguageSelectedEvent : EventUnitWrapper<AppSessionData.Language>
    {
        protected override string EventName => ScriptEventNames.LANGUAGE_SELECTED;
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On Lessons Spawned")]
    public class LessonsSpawnedEvent : EventUnitWrapper
    {
        protected override string EventName => ScriptEventNames.LESSONS_SPAWNED;
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On Start User Attempt")]
    public class StartUserAttemptEvent : EventUnitWrapper
    {
        protected override string EventName => ScriptEventNames.START_USER_LESSON_ATTEMPT;
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On User Attempted Lesson")]
    public class UserAttemptedLessonEvent : EventUnitWrapper<bool>
    {
        protected override string EventName => ScriptEventNames.USER_ATTEMPTED_LESSON;
    }

    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("On Lesson Tier Complete")]
    public class LessonTierCompleteEvent : EventUnitWrapper
    {
        protected override string EventName => ScriptEventNames.LESSON_TIER_COMPLETE;
    }
}