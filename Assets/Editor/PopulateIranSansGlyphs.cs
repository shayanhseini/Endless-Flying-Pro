using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

[InitializeOnLoad]
public static class PopulateIranSansGlyphs
{
    private const string TriggerPath = "Temp/trigger_fix_font.txt";

    static PopulateIranSansGlyphs()
    {
        EditorApplication.update += CheckTrigger;
        EditorApplication.delayCall += FixFontAsset;
    }

    private static void CheckTrigger()
    {
        if (!File.Exists(TriggerPath))
            return;

        try
        {
            File.Delete(TriggerPath);
        }
        catch
        {
            return;
        }

        FixFontAsset();
    }

    [MenuItem("Tools/Populate IranSans Glyphs")]
    public static void FixFontAsset()
    {
        string assetPath = "Assets/RTLTMPro/Fonts/IRANSansMobile Bold SDF Arabic.asset";
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset == null)
        {
            Debug.LogError("[PopulateIranSansGlyphs] Could not find font asset at " + assetPath);
            return;
        }

        // List of all unicodes required by RTLTMPro in the project
        uint[] requiredUnicodes = new uint[]
        {
            // All 36 characters used in GameScene.unity RTL strings
            32,     // Space
            64401,  // 0xFB91 (ک initial)
            64509,  // 0xFBFD (ی final)
            64510,  // 0xFBFE (ی isolated)
            64511,  // 0xFBFF (ی initial/medial)
            65165,  // 0xFE8D (ا isolated)
            65166,  // 0xFE8E (ا final)
            65169,  // 0xFE91 (ب initial)
            65174,  // 0xFE96 (ت final)
            65175,  // 0xFE97 (ت initial)
            65176,  // 0xFE98 (ت medial)
            65181,  // 0xFE9D (ج isolated/final)
            65184,  // 0xFEA0 (ج medial)
            65187,  // 0xFEA3 (ح initial)
            65191,  // 0xFEA7 (خ initial)
            65193,  // 0xFEA9 (د isolated)
            65194,  // 0xFEAA (د final)
            65197,  // 0xFEAD (ر isolated)
            65198,  // 0xFEAE (ر final)
            65199,  // 0xFEAF (ز isolated)
            65203,  // 0xFEB3 (س initial)
            65204,  // 0xFEB4 (س medial)
            65207,  // 0xFEB7 (ش initial)
            65225,  // 0xFEC9 (ع isolated)
            65227,  // 0xFECB (ع initial)
            65235,  // 0xFED3 (ف initial)
            65238,  // 0xFED6 (ق final)
            65246,  // 0xFEDE (ل final)
            65248,  // 0xFEE0 (ل medial)
            65251,  // 0xFEE3 (م initial)
            65254,  // 0xFEE6 (ن final)
            65255,  // 0xFEE7 (ن initial)
            65260,  // 0xFEEC (ه medial)
            65261,  // 0xFEED (و isolated)
            65262,  // 0xFEEE (و final)
            65275,  // 0xFEFB (لا isolated)

            // Additional Presentation Forms for UI (فعال, غیرفعال, آسان, سخت, متوسط)
            0xFE80, 0xFE81, 0xFE82, 0xFE83, 0xFE84, 0xFE85, 0xFE87, 0xFE88,
            0xFE89, 0xFE8A, 0xFE8B, 0xFE8C, 0xFE8F, 0xFE90, 0xFE92, 0xFE93,
            0xFE94, 0xFE95, 0xFE99, 0xFE9A, 0xFE9B, 0xFE9C, 0xFE9E, 0xFE9F,
            0xFEA1, 0xFEA2, 0xFEA4, 0xFEA5, 0xFEA6, 0xFEA8, 0xFEAB, 0xFEAC,
            0xFEB0, 0xFEB1, 0xFEB2, 0xFEB5, 0xFEB6, 0xFEB8, 0xFEB9, 0xFEBA,
            0xFEBB, 0xFEBC, 0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0, 0xFEC1, 0xFEC2,
            0xFEC3, 0xFEC4, 0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8, 0xFECA, 0xFECC,
            0xFECD, 0xFECE, 0xFECF, 0xFED0, 0xFED1, 0xFED2, 0xFED4, 0xFED5,
            0xFED7, 0xFED8, 0xFED9, 0xFEDA, 0xFEDB, 0xFEDC, 0xFEDD, 0xFEDF,
            0xFEE1, 0xFEE2, 0xFEE4, 0xFEE5, 0xFEE8, 0xFEE9, 0xFEEA, 0xFEEB,
            0xFEED, 0xFEEE, 0xFEEF, 0xFEF0, 0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4,
            0xFEFC, 0xFB56, 0xFB57, 0xFB58, 0xFB59, 0xFB7A, 0xFB7B, 0xFB7C,
            0xFB7D, 0xFB8A, 0xFB8B, 0xFB8E, 0xFB8F, 0xFB90, 0xFB92, 0xFB93,
            0xFB94, 0xFB95, 0xFBFC,

            // Basic Persian / Arabic characters
            0x0621, 0x0622, 0x0623, 0x0624, 0x0625, 0x0626, 0x0627, 0x0628,
            0x067E, 0x0629, 0x062A, 0x062B, 0x062C, 0x0686, 0x062D, 0x062E,
            0x062F, 0x0630, 0x0631, 0x0632, 0x0698, 0x0633, 0x0634, 0x0635,
            0x0636, 0x0637, 0x0638, 0x0639, 0x063A, 0x0641, 0x0642, 0x06A9,
            0x06AF, 0x0644, 0x0645, 0x0646, 0x0647, 0x0648, 0x06CC, 0x0649,
            0x064A, 0x200C, 0x200D,

            // Standard Digits
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            // Persian Digits
            0x06F0, 0x06F1, 0x06F2, 0x06F3, 0x06F4, 0x06F5, 0x06F6, 0x06F7, 0x06F8, 0x06F9,

            // ASCII Punctuation and symbols
            ':', '.', '-', '/', '+', '%', '(', ')', '[', ']', '!', '?', ',', ';',
            'K', 'M', 'H', 'S', 'P', 'E', 'D', 'W', 'N'
        };

        // Ensure asset is dynamic while adding characters
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        int alreadyPresent = 0;
        int added = 0;
        int failed = 0;

        foreach (uint u in requiredUnicodes)
        {
            if (fontAsset.HasCharacter((int)u))
            {
                alreadyPresent++;
                continue;
            }

            try
            {
                uint[] single = new uint[] { u };
                uint[] missing;
                if (fontAsset.TryAddCharacters(single, out missing))
                {
                    added++;
                }
                else
                {
                    failed++;
                }
            }
            catch (Exception ex)
            {
                // Silent catch on individual character collision to continue adding others
                failed++;
            }
        }

        // Freeze as Static so Unity never purges dynamic glyph cache on build or asset refresh
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PopulateIranSansGlyphs] Froze {fontAsset.name} as Static with {fontAsset.characterTable.Count} glyphs (Added: {added}, Existing: {alreadyPresent}).");
    }
}
