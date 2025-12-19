// Copyright (c) Meta Platforms, Inc. and affiliates.

package com.meta.samples.SpatialLingo;

import com.unity3d.player.UnityPlayerGameActivity;
import android.os.Bundle;
import android.os.Build;

public class SpatialLingoActivity extends UnityPlayerGameActivity {
    @Override protected String updateUnityCommandLineArguments(String cmdLine) {
        if (cmdLine == null)
            cmdLine = "";
        if (cmdLine.contains("-job-worker-count"))
            return cmdLine;
        return cmdLine + " -job-worker-count 4";
    }
}
