// Copyright (c) Meta Platforms, Inc. and affiliates.
package com.meta.psocache;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.net.Uri;
import android.util.Log;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import android.content.res.AssetManager;

// Extends ContentProvider in order to get the onCreate callback on library initialization.
// See: https://www.andretietz.com/2017/09/06/autoinitialise-android-library/

/**
 * Initializes the Vulkan PSO cache by copying an existing cache from the application.
 */
public class VulkanCacheInitializer extends ContentProvider {

    @Override
    public boolean onCreate() {
        Log.d("Unity", "Beginning Vulkan PSO cache initialization...");
        Log.d("Unity", "Device Info: " + android.os.Build.MODEL + " " + android.os.Build.DEVICE + " " + android.os.Build.PRODUCT);

        String cacheSuffix = "quest3";

        // Detect Quest 2 devices and use alternative cache
        if (android.os.Build.DEVICE.equals("hollywood"))
        {
            cacheSuffix = "quest2";
        }

        Log.d("Unity", "Using " + cacheSuffix + " variant...");

        // Unity 6
        Path androidFilePath = Paths.get(getContext().getDataDir().getAbsolutePath() + "/cache", "vulkan_pso_cache.bin");
        Log.d("Unity", "androidFilePath: " + androidFilePath);

        try (InputStream file = getContext().getAssets().open("vulkan_pso_cache_" + cacheSuffix + ".bin")) {
            Log.d("Unity", "asset file: " + file);
            if (Files.exists(androidFilePath)) {
                Log.d("Unity", "Vulkan PSO cache already exists at " + androidFilePath);
            } else {
                Log.d("Unity", "Copying Vulkan PSO cache to " + androidFilePath);
                Files.copy(file, androidFilePath);
            }
        } catch (IOException e) {
            Log.e("Unity", "Failed to initialize Vulkan PSO cache: " + e.toString());
        }

        return true;
    }

    @Override
    public Cursor query(Uri uri, String[] strings, String s, String[] strings1, String s1) {
        return null;
    }

    @Override
    public String getType(Uri uri) {
        return "";
    }

    @Override
    public Uri insert(Uri uri, ContentValues contentValues) {
        return null;
    }

    @Override
    public int delete(Uri uri, String s, String[] strings) {
        return 0;
    }

    @Override
    public int update(Uri uri, ContentValues contentValues, String s, String[] strings) {
        return 0;
    }
}
