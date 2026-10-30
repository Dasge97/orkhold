using UnityEditor;
using UnityEngine;

/// Imágenes de la interfaz en Resources/Ui (copias del UI Pack RPG y de Game Icons de Kenney, 15-interfaz.md §2):
/// sprites sin mipmaps, con bordes para estirarse (paneles y botones) sin deformar las esquinas.
public class ImportadorUi : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Ui/") && !assetPath.Contains("/Resources/Iconos/")) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (n.StartsWith("panel")) imp.spriteBorder = new Vector4(16, 16, 16, 16);
        else if (n.StartsWith("buttonLong")) imp.spriteBorder = new Vector4(14, 14, 14, 18);
        else if (n.StartsWith("buttonSquare") || n.StartsWith("buttonRound")) imp.spriteBorder = new Vector4(12, 14, 12, 16);
        else if (n.Contains("horizontalMid")) imp.spriteBorder = Vector4.zero;
    }
}
