using UnityEditor;
using UnityEngine;

namespace ChromaFrontier.Editor
{
    /// <summary>
    /// art/build.mjs 가 굽는 Resources/Art/** PNG 의 임포트 설정을 강제한다.
    /// PPU 64 (타일 마름모 128px = 2 유닛), 밉맵 없음, 무압축, Bilinear. 피벗은 폴더별 캔버스 규칙을 따른다 —
    /// art/*.mjs 의 캔버스 크기·바닥점을 바꾸면 여기 숫자도 같이 고칠 것.
    /// </summary>
    public sealed class ArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/Art/";
        public const float PixelsPerUnit = 64f;

        // tiles.mjs TILE: 136x104, 윗면 중심 (68, 34)
        private static readonly Vector2 TilePivot = new(0.5f, 1f - 34f / 104f);
        // units.mjs UNIT: 96x112, 바닥점 (48, 100)
        private static readonly Vector2 UnitPivot = new(0.5f, 12f / 112f);
        // tiles.mjs decor: 64x80, 바닥점 (32, 72)
        private static readonly Vector2 DecorPivot = new(0.5f, 8f / 80f);

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = PivotFor(assetPath);
            importer.SetTextureSettings(settings);
        }

        private static Vector2 PivotFor(string path)
        {
            var rel = path.Substring(Root.Length);
            if (rel.StartsWith("Tiles/")) return TilePivot;
            if (rel.StartsWith("Decor/")) return DecorPivot;
            if (rel.StartsWith("Units/unit_")) return UnitPivot;
            return new Vector2(0.5f, 0.5f);
        }
    }
}
