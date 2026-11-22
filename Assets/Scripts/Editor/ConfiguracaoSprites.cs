using UnityEditor;
using UnityEngine;

// Roda SOZINHO toda vez que a Unity importa uma imagem.
// Fica na pasta "Editor", então só existe dentro do editor e não vai para o jogo final.
public class ConfiguracaoSprites : AssetPostprocessor
{
    // 64 pixels = 1 unidade (1 metro) no mundo do jogo.
    // Assim cada bloco de chão (64x64) ocupa exatamente 1 quadrado da grade,
    // e a Zora (128x128) fica com a proporção original do pacote Kenney.
    const float PixelsPorUnidade = 64f;

    void OnPreprocessTexture()
    {
        // Só mexe nas imagens da pasta Art (não interfere em mais nada do projeto)
        if (!assetPath.StartsWith("Assets/Art/")) return;

        var importador = (TextureImporter)assetImporter;

        importador.textureType = TextureImporterType.Sprite;          // imagem 2D
        importador.spriteImportMode = SpriteImportMode.Single;        // 1 imagem = 1 sprite
        importador.spritePixelsPerUnit = PixelsPorUnidade;            // escala padronizada
        importador.filterMode = FilterMode.Bilinear;                  // bordas suaves (arte não é pixel art)
        importador.textureCompression = TextureImporterCompression.Uncompressed; // sem perder nitidez
        importador.mipmapEnabled = false;                             // desnecessário em 2D
        importador.alphaIsTransparency = true;                        // fundo transparente limpo
    }
}
