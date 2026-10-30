using System.Linq;
using UnityEditor;
using UnityEngine;

/// Ajusta la importación de los FBX de Kenney: rig genérico y clips en bucle donde toca.
public class ImportadorKenney : AssetPostprocessor
{
    static readonly string[] EnBucle = { "idle", "walk", "sprint", "static", "holding-right", "holding-left", "holding-both" };

    void OnPreprocessModel()
    {
        if (!assetPath.Contains("/Kenney/")) return;
        var imp = (ModelImporter)assetImporter;
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.importAnimation = true;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        imp.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.Contains("/Kenney/")) return;
        var imp = (ModelImporter)assetImporter;
        var clips = imp.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;
        foreach (var c in clips)
        {
            c.loopTime = EnBucle.Contains(c.name);
            c.lockRootRotation = true;
            c.lockRootHeightY = true;
            c.lockRootPositionXZ = true;   // el movimiento lo lleva el CharacterController, no el clip
            c.keepOriginalOrientation = true;
            c.keepOriginalPositionY = true;
            c.keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = clips;
    }

    void OnPreprocessTexture()
    {
        // Imágenes de interfaz: controles, paneles del UI Pack RPG e iconos.
        if (!assetPath.Contains("/Kenney/MobileControls/") && !assetPath.Contains("/Kenney/UIPackRPG/") && !assetPath.Contains("/Kenney/GameIcons/")) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
    }
}
