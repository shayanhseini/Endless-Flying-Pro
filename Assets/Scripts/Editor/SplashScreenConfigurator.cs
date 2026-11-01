#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SplashScreenConfigurator
{
    static SplashScreenConfigurator()
    {
        ConfigureSplashScreen();
    }

    [MenuItem("Tools/Configure Splash Screen")]
    public static void ConfigureSplashScreen()
    {
        string logoPath = "Assets/Texture/TopVision.ir Orbit Logo.png";
        Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
        
        if (logoSprite == null)
        {
            TextureImporter importer = AssetImporter.GetAtPath(logoPath) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                    logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
                }
            }
        }

        if (logoSprite != null)
        {
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);

            PlayerSettings.SplashScreenLogo[] logos = new PlayerSettings.SplashScreenLogo[]
            {
                PlayerSettings.SplashScreenLogo.Create(2.0f, logoSprite)
            };
            PlayerSettings.SplashScreen.logos = logos;

            AssetDatabase.SaveAssets();
            Debug.Log("[SplashScreenConfigurator] TopVision.ir Orbit Logo successfully set to Splash Screen for 2.0s!");
        }
        else
        {
            Debug.LogWarning("[SplashScreenConfigurator] Could not find or load: " + logoPath);
        }
    }
}
#endif
