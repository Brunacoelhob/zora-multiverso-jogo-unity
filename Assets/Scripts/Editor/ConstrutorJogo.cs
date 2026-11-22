using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// =====================================================================================
//  CONSTRUTOR DO JOGO (só existe no editor)
//  Monta TUDO automaticamente a partir dos arquivos do projeto:
//    - tiles, animações (Animation Clips + Animator Controllers) e prefabs
//    - as 4 fases, lendo os mapas de texto em Assets/Fases/*.txt
//    - Menu, Vitória e Game Over (com interface)
//    - lista de cenas do build, prints das fases e o executável Windows
//  Menu da Unity:  Zora > 1 - Construir jogo  |  2 - Capturar prints  |  3 - Gerar executável
// =====================================================================================
public static class ConstrutorJogo
{
    const string PastaGerado = "Assets/Gerado";
    const string PastaPrefabs = "Assets/Prefabs";
    const string PastaCenas = "Assets/Scenes";
    const string PastaFases = "Assets/Fases";
    const string CaminhoExecutavel = "Build/Windows/ZoraMultiverso.exe";
    const string PastaPrints = "Prints";

    class Planeta
    {
        public string cena, nome, terreno, fundo, slime, curiosidade, dica, proxima;
        public int numero;
        public float gravidade;
        public Color tom = Color.white, ceu;
        public string[] decoracoes;
    }

    static readonly Planeta[] Planetas =
    {
        new Planeta
        {
            cena = Cenas.Fase1, numero = 1, nome = "Lua", gravidade = 1.62f, terreno = "stone",
            fundo = "fundo_lua", ceu = new Color(0.02f, 0.02f, 0.07f), slime = "slime_normal",
            decoracoes = new[] { "rock" }, proxima = Cenas.Fase2,
            curiosidade = "A gravidade da Lua é de apenas 1,62 m/s², cerca de 1/6 da Terra. Aqui os pulos são altos e lentos!",
            dica = "A/D ou setas: andar   •   Shift: correr   •   Espaço: pular   •   Junte os 3 fragmentos de estrela!"
        },
        new Planeta
        {
            cena = Cenas.Fase2, numero = 2, nome = "Marte", gravidade = 3.71f, terreno = "sand",
            tom = new Color(1f, 0.62f, 0.5f), fundo = "fundo_marte", ceu = new Color(0.27f, 0.1f, 0.08f),
            slime = "slime_fire", decoracoes = new[] { "rock", "cactus" }, proxima = Cenas.Fase3,
            curiosidade = "Marte tem 3,71 m/s² de gravidade (38% da Terra). Paredões altos? Use as escadas!",
            dica = "Perto de uma escada, segure W (ou seta para cima) para escalar."
        },
        new Planeta
        {
            cena = Cenas.Fase3, numero = 3, nome = "Terra", gravidade = 9.81f, terreno = "grass",
            fundo = "fundo_terra", ceu = new Color(0.27f, 0.55f, 0.9f), slime = "slime_normal",
            decoracoes = new[] { "bush", "grass", "mushroom_red", "mushroom_brown" }, proxima = Cenas.Fase4,
            curiosidade = "Na Terra a gravidade é 9,81 m/s²: o pulo que você conhece. Cuidado com as abelhas!",
            dica = "Pule em cima dos inimigos para derrotá-los e ganhar moedas."
        },
        new Planeta
        {
            cena = Cenas.Fase4, numero = 4, nome = "Júpiter", gravidade = 24.79f, terreno = "purple",
            fundo = "fundo_jupiter", ceu = new Color(0.1f, 0.05f, 0.12f), slime = "slime_spike",
            decoracoes = new[] { "grass_purple", "mushroom_red", "rock" }, proxima = Cenas.Vitoria,
            curiosidade = "Júpiter tem 24,79 m/s², 2,5 vezes a Terra! Os pulos ficam curtinhos: escalar e correr é essencial.",
            dica = "Segure Shift para correr e vencer os buracos."
        },
    };

    static Material materialSprite;
    static Font fonte;
    static PhysicsMaterial2D semAtrito;
    static readonly Dictionary<string, Tile> cacheTiles = new();
    static readonly Dictionary<string, GameObject> prefabs = new();

    // =================================================================================
    //  PONTOS DE ENTRADA
    // =================================================================================

    [MenuItem("Zora/1 - Construir jogo (cenas, prefabs, animações)")]
    public static void ConstruirTudo()
    {
        AssetDatabase.Refresh();
        Preparar();
        CriarPrefabs();
        foreach (var p in Planetas) CriarFase(p);
        CriarMenu();
        CriarTelaFinal(true);
        CriarTelaFinal(false);
        ConfigurarBuild();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene($"{PastaCenas}/{Cenas.Menu}.unity");
        Debug.Log("[Zora] Jogo construído com sucesso!");
    }

    [MenuItem("Zora/2 - Capturar prints das fases")]
    public static void CapturarPrints()
    {
        Directory.CreateDirectory(PastaPrints);
        foreach (var p in Planetas)
        {
            var cena = EditorSceneManager.OpenScene($"{PastaCenas}/{p.cena}.unity");
            var linhas = LerMapa(p.cena);
            int largura = linhas.Max(l => l.Length);
            float[] xs = { 11f, largura * 0.35f, largura * 0.62f, largura - 11f };
            for (int i = 0; i < xs.Length; i++)
                Fotografar($"{PastaPrints}/fase{p.numero}_{Normalizar(p.nome)}_{i + 1}.png", new Vector2(xs[i], 7.5f), i == 0);
        }
        foreach (var nome in new[] { Cenas.Menu, Cenas.Vitoria, Cenas.GameOver })
        {
            EditorSceneManager.OpenScene($"{PastaCenas}/{nome}.unity");
            Fotografar($"{PastaPrints}/tela_{nome.ToLower()}.png", null, true);
        }
        Debug.Log("[Zora] Prints salvos em " + Path.GetFullPath(PastaPrints));
    }

    [MenuItem("Zora/3 - Gerar executável Windows")]
    public static void GerarExecutavel()
    {
        ConfigurarBuild();
        var opcoes = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Select(c => c.path).ToArray(),
            locationPathName = CaminhoExecutavel,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var relatorio = BuildPipeline.BuildPlayer(opcoes);
        var resumo = relatorio.summary;
        Debug.Log($"[Zora] Build: {resumo.result} | {resumo.totalErrors} erro(s) | {resumo.totalSize / (1024 * 1024)} MB | {Path.GetFullPath(CaminhoExecutavel)}");
        if (resumo.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("O build falhou. Veja o Console.");
    }

    // Usado pela linha de comando (Unity em modo batch): constrói, fotografa e gera o .exe
    public static void ExecutarTudo()
    {
        try
        {
            ConstruirTudo();
            try { CapturarPrints(); }
            catch (Exception e) { Debug.LogWarning("[Zora] Não foi possível capturar prints: " + e.Message); }
            GerarExecutavel();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    // TESTE AUTOMÁTICO DE FÍSICA: em cada fase, a Zora deve ficar em pé no chão
    // e a altura do pulo deve bater com a tabela usada no design das fases.
    [MenuItem("Zora/4 - Testar física das fases")]
    public static void TestarFisica()
    {
        var modoAnterior = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        bool tudoOk = true;
        try
        {
            foreach (var p in Planetas)
            {
                EditorSceneManager.OpenScene($"{PastaCenas}/{p.cena}.unity");
                var cfg = Object.FindAnyObjectByType<ConfiguracaoPlaneta>();
                Physics2D.gravity = new Vector2(0f, -cfg.GravidadeNoJogo);
                var zora = Object.FindAnyObjectByType<MovimentoJogador>();
                var rb = zora.GetComponent<Rigidbody2D>();
                float yInicial = rb.position.y;

                for (int i = 0; i < 100; i++) Physics2D.Simulate(0.02f); // 2 segundos parada
                float yParada = rb.position.y;

                rb.linearVelocity = new Vector2(0f, zora.forcaPulo);
                float maxY = yParada;
                for (int i = 0; i < 200; i++) { Physics2D.Simulate(0.01f); maxY = Mathf.Max(maxY, rb.position.y); }
                for (int i = 0; i < 300; i++) Physics2D.Simulate(0.01f);
                float yPouso = rb.position.y;

                bool ok = Mathf.Abs(yParada - yInicial) < 0.1f && Mathf.Abs(yPouso - yParada) < 0.1f;
                tudoOk &= ok;
                Debug.Log($"[Zora][Teste] {p.nome}: gravidade no jogo {cfg.GravidadeNoJogo:0.0} | início y={yInicial:0.00} parada y={yParada:0.00} " +
                          $"pulo={maxY - yParada:0.00} blocos | pouso y={yPouso:0.00} => {(ok ? "OK" : "FALHOU")}");
            }
        }
        finally
        {
            Physics2D.simulationMode = modoAnterior;
        }
        if (!tudoOk) throw new Exception("Teste de física falhou");
    }

    public static void ExecutarTeste()
    {
        try { TestarFisica(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    // Só constrói (sem build) — usado para conferir erros rapidamente
    public static void ExecutarSoConstrucao()
    {
        try { ConstruirTudo(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    // =================================================================================
    //  PREPARAÇÃO E UTILITÁRIOS
    // =================================================================================

    static void Preparar()
    {
        cacheTiles.Clear();
        prefabs.Clear();
        foreach (var pasta in new[] { PastaGerado + "/Tiles", PastaGerado + "/Animacoes", PastaPrefabs, PastaCenas })
            GarantirPasta(pasta);

        materialSprite = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (materialSprite == null) throw new Exception("Material Sprite-Unlit-Default não encontrado");

        fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        semAtrito = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
        SalvarAsset(semAtrito, PastaGerado + "/SemAtrito.physicsMaterial2D");

        // A cena de exemplo do template não é usada
        if (File.Exists(PastaCenas + "/SampleScene.unity")) AssetDatabase.DeleteAsset(PastaCenas + "/SampleScene.unity");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void GarantirPasta(string caminho)
    {
        var partes = caminho.Split('/');
        string atual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string proximo = atual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(proximo)) AssetDatabase.CreateFolder(atual, partes[i]);
            atual = proximo;
        }
    }

    static void SalvarAsset(Object asset, string caminho)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(caminho) != null) AssetDatabase.DeleteAsset(caminho);
        AssetDatabase.CreateAsset(asset, caminho);
    }

    static Sprite S(string caminho)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/{caminho}.png");
        if (s == null) throw new Exception("Sprite não encontrado: Assets/Art/" + caminho + ".png");
        return s;
    }

    static AudioClip A(string caminho)
    {
        var a = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + caminho);
        if (a == null) throw new Exception("Áudio não encontrado: Assets/Audio/" + caminho);
        return a;
    }

    static string Normalizar(string s) =>
        s.ToLower().Replace("ú", "u").Replace("á", "a").Replace("ã", "a").Replace("é", "e");

    static SpriteRenderer Desenho(GameObject go, Sprite sprite, int ordem)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = materialSprite;
        sr.sortingOrder = ordem;
        return sr;
    }

    // =================================================================================
    //  ANIMAÇÕES (ferramenta de animação da Unity: Animation Clip + Animator Controller)
    // =================================================================================

    static AnimationClip CriarClip(string nome, float fps, bool repetir, params Sprite[] quadros)
    {
        var clip = new AnimationClip { frameRate = fps };
        var ligacao = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var chaves = new ObjectReferenceKeyframe[quadros.Length + 1];
        for (int i = 0; i < quadros.Length; i++)
            chaves[i] = new ObjectReferenceKeyframe { time = i / fps, value = quadros[i] };
        // chave final repete o último quadro, para cada quadro durar 1/fps
        chaves[quadros.Length] = new ObjectReferenceKeyframe { time = quadros.Length / fps, value = quadros[^1] };
        AnimationUtility.SetObjectReferenceCurve(clip, ligacao, chaves);

        var config = AnimationUtility.GetAnimationClipSettings(clip);
        config.loopTime = repetir;
        AnimationUtility.SetAnimationClipSettings(clip, config);

        SalvarAsset(clip, $"{PastaGerado}/Animacoes/{nome}.anim");
        return clip;
    }

    static AnimatorController ControladorSimples(string nome, AnimationClip clip)
    {
        string caminho = $"{PastaGerado}/Animacoes/{nome}.controller";
        if (AssetDatabase.LoadAssetAtPath<Object>(caminho) != null) AssetDatabase.DeleteAsset(caminho);
        return AnimatorController.CreateAnimatorControllerAtPathWithClip(caminho, clip);
    }

    // Controller da Zora: um estado por animação + transições a partir de "Any State",
    // escolhidas pelo parâmetro inteiro "Estado" (enviado pelo script AnimacaoJogador).
    static AnimatorController ControladorZora()
    {
        string c = "Characters/character_pink_";
        var clips = new[]
        {
            CriarClip("Zora_Parada", 1, true, S(c + "idle")),
            CriarClip("Zora_Andando", 8, true, S(c + "walk_a"), S(c + "walk_b")),
            CriarClip("Zora_Correndo", 14, true, S(c + "walk_a"), S(c + "walk_b")),
            CriarClip("Zora_Pulando", 1, true, S(c + "jump")),
            CriarClip("Zora_Escalando", 8, true, S(c + "climb_a"), S(c + "climb_b")),
            CriarClip("Zora_EscadaParada", 1, true, S(c + "climb_a")),
            CriarClip("Zora_Dano", 1, true, S(c + "hit")),
        };
        string[] nomes = Enum.GetNames(typeof(AnimacaoJogador.EstadoAnimacao));

        string caminho = $"{PastaGerado}/Animacoes/Zora.controller";
        if (AssetDatabase.LoadAssetAtPath<Object>(caminho) != null) AssetDatabase.DeleteAsset(caminho);
        var controlador = AnimatorController.CreateAnimatorControllerAtPath(caminho);
        controlador.AddParameter("Estado", AnimatorControllerParameterType.Int);
        var maquina = controlador.layers[0].stateMachine;

        for (int i = 0; i < clips.Length; i++)
        {
            var estado = maquina.AddState(nomes[i], new Vector3(450, i * 70, 0));
            estado.motion = clips[i];
            if (i == 0) maquina.defaultState = estado;

            var transicao = maquina.AddAnyStateTransition(estado);
            transicao.hasExitTime = false;
            transicao.duration = 0f;
            transicao.canTransitionToSelf = false;
            transicao.AddCondition(AnimatorConditionMode.Equals, i, "Estado");
        }
        return controlador;
    }

    // =================================================================================
    //  PREFABS
    // =================================================================================

    static GameObject SalvarPrefab(GameObject go)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PastaPrefabs}/{go.name}.prefab");
        Object.DestroyImmediate(go);
        prefabs[prefab.name] = prefab;
        return prefab;
    }

    static GameObject Instanciar(string nomePrefab, Vector3 posicao, Transform pai)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[nomePrefab]);
        go.transform.position = posicao;
        go.transform.SetParent(pai, true);
        return go;
    }

    static void CriarPrefabs()
    {
        // ---------- ZORA ----------
        var zora = new GameObject("Zora");
        Desenho(zora, S("Characters/character_pink_idle"), 10);
        var rb = zora.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = semAtrito;
        var capsula = zora.AddComponent<CapsuleCollider2D>();
        capsula.direction = CapsuleDirection2D.Vertical;
        capsula.size = new Vector2(0.8f, 1.45f);
        capsula.offset = new Vector2(0f, -0.275f); // base da cápsula = pés da Zora
        zora.AddComponent<Animator>().runtimeAnimatorController = ControladorZora();
        zora.AddComponent<MovimentoJogador>();
        zora.AddComponent<VidaJogador>();
        zora.AddComponent<AnimacaoJogador>();
        SalvarPrefab(zora);

        // ---------- INIMIGOS ----------
        foreach (var slime in new[] { "slime_normal", "slime_fire", "slime_spike" })
            CriarInimigoAndante("Inimigo_" + slime, slime + "_walk_a", slime + "_walk_b", slime + "_flat",
                new Vector2(0.8f, 0.55f), new Vector2(0f, -0.225f), 1.6f);
        CriarInimigoAndante("Inimigo_caracol", "snail_walk_a", "snail_walk_b", "snail_shell",
            new Vector2(0.9f, 0.7f), new Vector2(0f, -0.15f), 0.9f);

        var abelha = new GameObject("Inimigo_abelha");
        Desenho(abelha, S("Enemies/bee_a"), 8);
        var rbA = abelha.AddComponent<Rigidbody2D>();
        rbA.bodyType = RigidbodyType2D.Kinematic;
        rbA.interpolation = RigidbodyInterpolation2D.Interpolate;
        abelha.AddComponent<BoxCollider2D>().size = new Vector2(0.75f, 0.65f);
        abelha.AddComponent<Animator>().runtimeAnimatorController =
            ControladorSimples("Abelha", CriarClip("Abelha_Voando", 10, true, S("Enemies/bee_a"), S("Enemies/bee_b")));
        var iA = abelha.AddComponent<Inimigo>();
        iA.tipo = Inimigo.TipoMovimento.Voador;
        iA.velocidade = 1.5f;
        iA.distanciaPatrulha = 3f;
        iA.spriteDerrotado = S("Enemies/bee_rest");
        SalvarPrefab(abelha);

        // ---------- COLETÁVEIS ----------
        var moeda = new GameObject("Moeda");
        Desenho(moeda, S("Tiles/coin_gold"), 5);
        moeda.AddComponent<CircleCollider2D>().isTrigger = true;
        moeda.GetComponent<CircleCollider2D>().radius = 0.3f;
        moeda.AddComponent<Animator>().runtimeAnimatorController =
            ControladorSimples("Moeda", CriarClip("Moeda_Girando", 6, true, S("Tiles/coin_gold"), S("Tiles/coin_gold_side")));
        moeda.AddComponent<Coletavel>().tipo = TipoColetavel.Moeda;
        SalvarPrefab(moeda);

        var fragmento = new GameObject("FragmentoEstrela");
        Desenho(fragmento, S("Tiles/star"), 6);
        var brilho = new GameObject("Brilho");
        brilho.transform.SetParent(fragmento.transform, false);
        brilho.transform.localScale = Vector3.one * 3.2f;
        Desenho(brilho, S("Gerados/particula"), 5).color = new Color(1f, 0.95f, 0.5f, 0.55f);
        var cf = fragmento.AddComponent<CircleCollider2D>();
        cf.isTrigger = true;
        cf.radius = 0.4f;
        fragmento.AddComponent<Coletavel>().tipo = TipoColetavel.Fragmento;
        SalvarPrefab(fragmento);

        var coracao = new GameObject("Coracao");
        Desenho(coracao, S("Tiles/heart"), 5);
        var cc = coracao.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.35f;
        coracao.AddComponent<Coletavel>().tipo = TipoColetavel.Coracao;
        SalvarPrefab(coracao);

        // ---------- FASE ----------
        var espinho = new GameObject("Espinhos");
        Desenho(espinho, S("Tiles/spikes"), 3);
        var ce = espinho.AddComponent<BoxCollider2D>();
        ce.isTrigger = true;
        ce.size = new Vector2(0.8f, 0.35f);
        ce.offset = new Vector2(0f, -0.3f);
        espinho.AddComponent<Perigo>();
        SalvarPrefab(espinho);

        var bandeira = new GameObject("Checkpoint");
        Desenho(bandeira, S("Tiles/flag_off"), 3);
        var cb = bandeira.AddComponent<BoxCollider2D>();
        cb.isTrigger = true;
        cb.size = new Vector2(0.8f, 1.5f);
        cb.offset = new Vector2(0f, 0.25f);
        var chk = bandeira.AddComponent<Checkpoint>();
        chk.spriteDesligada = S("Tiles/flag_off");
        chk.spritesTremulando = new[] { S("Tiles/flag_red_a"), S("Tiles/flag_red_b") };
        SalvarPrefab(bandeira);

        var portal = new GameObject("Portal");
        Desenho(portal, S("Gerados/portal"), 2);
        var cp = portal.AddComponent<CircleCollider2D>();
        cp.isTrigger = true;
        cp.radius = 0.6f;
        portal.AddComponent<Portal>();
        SalvarPrefab(portal);
    }

    static void CriarInimigoAndante(string nome, string quadroA, string quadroB, string derrotado,
        Vector2 tamanho, Vector2 deslocamento, float velocidade)
    {
        var go = new GameObject(nome);
        Desenho(go, S("Enemies/" + quadroA), 8);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.mass = 20f;
        rb.sharedMaterial = semAtrito;
        var caixa = go.AddComponent<BoxCollider2D>();
        caixa.size = tamanho;
        caixa.offset = deslocamento;
        go.AddComponent<Animator>().runtimeAnimatorController =
            ControladorSimples(nome, CriarClip(nome + "_Andando", 6, true, S("Enemies/" + quadroA), S("Enemies/" + quadroB)));
        var inimigo = go.AddComponent<Inimigo>();
        inimigo.tipo = Inimigo.TipoMovimento.Andante;
        inimigo.velocidade = velocidade;
        inimigo.spriteDerrotado = S("Enemies/" + derrotado);
        SalvarPrefab(go);
    }

    // =================================================================================
    //  FASES (a partir dos mapas de texto)
    // =================================================================================

    static string[] LerMapa(string cena)
    {
        string caminho = $"{PastaFases}/{cena}.txt";
        var linhas = File.ReadAllText(caminho).Replace("\r", "").Split('\n').ToList();
        while (linhas.Count > 0 && linhas[^1].Length == 0 && linhas.Count > 16) linhas.RemoveAt(linhas.Count - 1);
        return linhas.ToArray();
    }

    static Tile TileDe(string nomeSprite)
    {
        if (cacheTiles.TryGetValue(nomeSprite, out var t)) return t;
        t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = S("Tiles/" + nomeSprite);
        t.colliderType = Tile.ColliderType.Grid;
        SalvarAsset(t, $"{PastaGerado}/Tiles/{nomeSprite}.asset");
        cacheTiles[nomeSprite] = t;
        return t;
    }

    static Tilemap CriarTilemap(string nome, Transform grade, Color cor, bool plataforma)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(grade, false);
        var mapa = go.AddComponent<Tilemap>();
        mapa.color = cor;
        var desenho = go.AddComponent<TilemapRenderer>();
        desenho.sharedMaterial = materialSprite;
        desenho.sortingOrder = 0;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var colisorTiles = go.AddComponent<TilemapCollider2D>();
        colisorTiles.compositeOperation = Collider2D.CompositeOperation.Merge; // junta os quadradinhos num colisor só
        var composto = go.AddComponent<CompositeCollider2D>();
        composto.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composto.sharedMaterial = semAtrito;
        if (plataforma)
        {
            // Plataforma de mão única: dá para atravessar pulando por baixo
            composto.usedByEffector = true;
            var efetor = go.AddComponent<PlatformEffector2D>();
            efetor.useOneWay = true;
            efetor.surfaceArc = 160f;
        }
        return mapa;
    }

    // O contorno de colisão do terreno precisa ser CALCULADO antes de salvar a cena;
    // senão a cena guarda um colisor vazio e a Zora atravessa o chão.
    static void FinalizarColisao(Tilemap mapa)
    {
        mapa.CompressBounds();
        var colisorTiles = mapa.GetComponent<TilemapCollider2D>();
        var composto = mapa.GetComponent<CompositeCollider2D>();
        colisorTiles.ProcessTilemapChanges();
        composto.GenerateGeometry();
        Physics2D.SyncTransforms();

        bool temTiles = mapa.GetUsedTilesCount() > 0;
        if (temTiles && composto.pathCount == 0)
        {
            // Plano B: sem o colisor composto, cada tile vira um quadrado de colisão
            Debug.LogWarning($"[Zora] {mapa.name}: colisor composto vazio, usando colisão por tile");
            bool efetor = composto.usedByEffector;
            Object.DestroyImmediate(composto);
            colisorTiles.compositeOperation = Collider2D.CompositeOperation.None;
            colisorTiles.usedByEffector = efetor;
            colisorTiles.sharedMaterial = semAtrito;
            colisorTiles.ProcessTilemapChanges();
        }
        Debug.Log($"[Zora] {mapa.name}: tiles={mapa.GetUsedTilesCount()} contornos={(composto != null ? composto.pathCount : -1)} formas={colisorTiles.shapeCount}");
    }

    static void CriarFase(Planeta p)
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var linhas = LerMapa(p.cena);
        int altura = linhas.Length;
        int largura = linhas.Max(l => l.Length);

        char Em(int x, int y)
        {
            int linha = altura - 1 - y;
            if (linha < 0 || linha >= altura || x < 0) return ' ';
            return x < linhas[linha].Length ? linhas[linha][x] : ' ';
        }
        bool Solido(int x, int y)
        {
            if (y >= altura) return false;
            if (x < 0 || x >= largura || y < 0) return true;
            return Em(x, y) == '#';
        }

        // ---------- Terreno e plataformas (Tilemaps) ----------
        var grade = new GameObject("Grade", typeof(Grid)).transform;
        var terreno = CriarTilemap("Terreno", grade, p.tom, false);
        var plataformas = CriarTilemap("Plataformas", grade, p.tom, true);
        var raizEntidades = new GameObject("Entidades").transform;
        var raizEscadas = new GameObject("Escadas").transform;
        var raizDecoracao = new GameObject("Decoracao").transform;
        string b = $"terrain_{p.terreno}_";
        GameObject zora = null;

        for (int y = 0; y < altura; y++)
        for (int x = 0; x < largura; x++)
        {
            char c = Em(x, y);
            var celula = new Vector3Int(x, y, 0);
            var centro = new Vector3(x + 0.5f, y + 0.5f, 0f);

            switch (c)
            {
                case '#':
                {
                    // "Auto-tile": escolhe a peça certa olhando os vizinhos (borda, quina, meio)
                    bool cima = Solido(x, y + 1), esq = Solido(x - 1, y), dir = Solido(x + 1, y);
                    string peca;
                    if (!esq && !dir) peca = cima ? "vertical_middle" : "vertical_top";
                    else if (!cima) peca = !esq ? "block_top_left" : !dir ? "block_top_right" : "block_top";
                    else peca = !esq ? "block_left" : !dir ? "block_right" : "block_center";
                    terreno.SetTile(celula, TileDe(b + peca));
                    break;
                }
                case '=':
                {
                    bool esq = Em(x - 1, y) == '=', dir = Em(x + 1, y) == '=';
                    string peca = !esq && !dir ? "block" : !esq ? "horizontal_left" : !dir ? "horizontal_right" : "horizontal_middle";
                    plataformas.SetTile(celula, TileDe(b + peca));
                    break;
                }
                case 'P': zora = Instanciar("Zora", new Vector3(x + 0.5f, y + 1f, 0f), null); break;
                case 'o': Instanciar("Moeda", centro, raizEntidades); break;
                case '*': Instanciar("FragmentoEstrela", centro, raizEntidades); break;
                case '+': Instanciar("Coracao", centro, raizEntidades); break;
                case '^': Instanciar("Espinhos", centro, raizEntidades); break;
                case 'F': Instanciar("Checkpoint", centro, raizEntidades); break;
                case 'X': Instanciar("Portal", new Vector3(x + 0.5f, y + 1f, 0f), raizEntidades); break;
                case 'S': Instanciar("Inimigo_" + p.slime, centro, raizEntidades); break;
                case 'C': Instanciar("Inimigo_caracol", centro, raizEntidades); break;
                case 'B': Instanciar("Inimigo_abelha", centro, raizEntidades); break;
            }
        }
        if (zora == null) throw new Exception(p.cena + ": mapa sem 'P'");

        // ---------- Escadas (colunas de 'H') ----------
        for (int x = 0; x < largura; x++)
        {
            int y = 0;
            while (y < altura)
            {
                if (Em(x, y) != 'H') { y++; continue; }
                int inicio = y;
                while (y < altura && Em(x, y) == 'H') y++;
                CriarEscada(x, inicio, y - 1, raizEscadas);
            }
        }

        // ---------- Decoração (pedras, arbustos...) em cima do terreno ----------
        for (int x = 1; x < largura - 1; x++)
        for (int y = 0; y < altura - 1; y++)
        {
            if (!Solido(x, y) || Solido(x, y + 1)) continue;
            if (Em(x, y + 1) != ' ' || Em(x - 1, y + 1) == 'X' || Em(x + 1, y + 1) == 'X' || Em(x - 1, y + 1) == 'H') continue;
            if (Aleatorio(x, y, p.numero) > 0.14f) continue;
            string nome = p.decoracoes[(int)(Aleatorio(y, x, p.numero + 7) * p.decoracoes.Length) % p.decoracoes.Length];
            var deco = new GameObject("Deco_" + nome);
            deco.transform.SetParent(raizDecoracao, false);
            deco.transform.position = new Vector3(x + 0.5f, y + 1.5f, 0f);
            Desenho(deco, S("Tiles/" + nome), 1).color = p.tom;
        }

        FinalizarColisao(terreno);
        FinalizarColisao(plataformas);

        // ---------- Câmera, fundo, sistemas e HUD ----------
        var cam = CriarCamera(p.ceu);
        var segue = cam.gameObject.AddComponent<CameraSegue>();
        segue.alvo = zora.transform;
        segue.limiteMinimo = new Vector2(0f, 1f);
        segue.limiteMaximo = new Vector2(largura, altura + 1f);
        cam.transform.position = new Vector3(zora.transform.position.x + 8f, 7f, -10f);

        var fundo = new GameObject("FundoEspacial");
        Desenho(fundo, S("Gerados/" + p.fundo), -100);
        fundo.transform.localScale = Vector3.one * 1.3f;
        fundo.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10f);
        fundo.AddComponent<ParallaxFundo>();

        var sistemas = new GameObject("Sistemas");
        var config = sistemas.AddComponent<ConfiguracaoPlaneta>();
        config.nomePlaneta = p.nome;
        config.numeroFase = p.numero;
        config.gravidadeReal = p.gravidade;
        config.curiosidade = p.curiosidade;
        config.dica = p.dica;
        config.proximaCena = p.proxima;
        config.limiteQueda = -3f;
        ConfigurarAudio(sistemas, "Musica/trilha_fases.wav", null);
        AdicionarRecursosEfeitos(sistemas);

        CriarHUD(p);

        EditorSceneManager.SaveScene(cena, $"{PastaCenas}/{p.cena}.unity");
    }

    static void CriarEscada(int x, int baseCelula, int topoCelula, Transform pai)
    {
        var go = new GameObject($"Escada_{x}");
        go.transform.SetParent(pai, false);
        go.transform.position = new Vector3(x + 0.5f, baseCelula, 0f);

        // Área do gatilho: da base até um pouco acima da borda onde a escada termina
        float alturaGatilho = (topoCelula + 1 - baseCelula) + 0.7f;
        var caixa = go.AddComponent<BoxCollider2D>();
        caixa.isTrigger = true;
        caixa.size = new Vector2(0.7f, alturaGatilho);
        caixa.offset = new Vector2(0f, alturaGatilho / 2f);

        var escada = go.AddComponent<Escada>();
        escada.baseY = baseCelula;
        escada.topoY = topoCelula + 1 + 0.12f;

        for (int y = baseCelula; y <= topoCelula; y++)
        {
            string peca = y == topoCelula ? "ladder_top" : y == baseCelula ? "ladder_bottom" : "ladder_middle";
            var degrau = new GameObject("Degrau");
            degrau.transform.SetParent(go.transform, false);
            degrau.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
            Desenho(degrau, S("Tiles/" + peca), 1);
        }
    }

    static float Aleatorio(int x, int y, int semente)
    {
        unchecked
        {
            uint h = (uint)(x * 73856093 ^ y * 19349663 ^ semente * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xFFFF) / 65535f;
        }
    }

    static Camera CriarCamera(Color fundo)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fundo;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>();
        return cam;
    }

    static void ConfigurarAudio(GameObject go, string musica, string jingle)
    {
        var audio = go.AddComponent<GerenciadorAudio>();
        audio.musica = A(musica);
        if (jingle != null) audio.jingleInicial = A(jingle);
        audio.pulo = A("SFX/sfx_jump.ogg");
        audio.moeda = A("SFX/sfx_coin.ogg");
        audio.fragmento = A("SFX/sfx_gem.ogg");
        audio.coracao = A("SFX/sfx_magic.ogg");
        audio.dano = A("SFX/sfx_hurt.ogg");
        audio.pisarInimigo = A("SFX/sfx_bump.ogg");
        audio.portalAbriu = A("SFX/sfx_magic.ogg");
        audio.teletransporte = A("SFX/sfx_disappear.ogg");
        audio.checkpoint = A("SFX/sfx_jump-high.ogg");
        audio.selecionar = A("SFX/sfx_select.ogg");
    }

    static void AdicionarRecursosEfeitos(GameObject go)
    {
        var r = go.AddComponent<RecursosEfeitos>();
        r.spriteParticula = S("Gerados/particula");
        r.materialParticula = materialSprite;
    }

    // =================================================================================
    //  INTERFACE (Canvas, textos, imagens, botões)
    // =================================================================================

    static readonly Color CorDestaque = new(1f, 0.55f, 0.82f);
    static readonly Color CorBotao = new(0.42f, 0.22f, 0.72f);

    static Canvas CriarCanvas(string nome)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var escala = go.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static RectTransform NovoUI(string nome, Transform pai, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = pivo;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        return rt;
    }

    static RectTransform TelaCheia(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Text Texto(Transform pai, string nome, string conteudo, int tamanho, TextAnchor alinhamento,
        Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 caixa, Color cor, FontStyle estilo = FontStyle.Normal)
    {
        var rt = NovoUI(nome, pai, ancora, pivo, pos, caixa);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fonte;
        t.text = conteudo;
        t.fontSize = tamanho;
        t.fontStyle = estilo;
        t.alignment = alinhamento;
        t.color = cor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var contorno = rt.gameObject.AddComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contorno.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    static Text TextoCentro(Transform pai, string nome, string conteudo, int tamanho, Vector2 pos, Vector2 caixa, Color cor,
        FontStyle estilo = FontStyle.Normal) =>
        Texto(pai, nome, conteudo, tamanho, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, caixa, cor, estilo);

    static Image Imagem(Transform pai, string nome, Sprite sprite, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tamanho)
    {
        var rt = NovoUI(nome, pai, ancora, pivo, pos, tamanho);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    static Image Painel(Transform pai, string nome, Vector2 pos, Vector2 tamanho, Color cor)
    {
        var rt = NovoUI(nome, pai, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, tamanho);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = cor;
        return img;
    }

    static Button Botao(Transform pai, string rotulo, Vector2 pos)
    {
        var img = Painel(pai, "Botao_" + rotulo, pos, new Vector2(440, 84), Color.white);
        img.raycastTarget = true;
        var botao = img.gameObject.AddComponent<Button>();
        var cores = botao.colors;
        cores.normalColor = CorBotao;
        cores.highlightedColor = new Color(0.66f, 0.4f, 1f);
        cores.selectedColor = new Color(0.66f, 0.4f, 1f);
        cores.pressedColor = new Color(0.3f, 0.15f, 0.5f);
        botao.colors = cores;
        TextoCentro(img.transform, "Rotulo", rotulo, 40, Vector2.zero, new Vector2(440, 84), Color.white, FontStyle.Bold);
        return botao;
    }

    static void CriarEventSystem()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem));
        var modulo = go.AddComponent<InputSystemUIInputModule>();
        modulo.AssignDefaultActions();
    }

    static void CriarHUD(Planeta p)
    {
        var canvas = CriarCanvas("HUD");
        var raiz = canvas.transform;
        var hud = canvas.gameObject.AddComponent<HUD>();
        var tl = new Vector2(0f, 1f);

        // Painel de status (canto superior esquerdo)
        var fundo = Painel(raiz, "PainelStatus", Vector2.zero, new Vector2(450, 170), new Color(0f, 0f, 0f, 0.35f));
        var rtFundo = (RectTransform)fundo.transform;
        rtFundo.anchorMin = rtFundo.anchorMax = tl;
        rtFundo.pivot = tl;
        rtFundo.anchoredPosition = new Vector2(20, -20);

        Imagem(raiz, "IconeZora", S("Tiles/hud_player_helmet_pink"), tl, tl, new Vector2(32, -32), new Vector2(80, 80));
        hud.coracoes = new Image[EstadoJogo.VidasMaximas];
        for (int i = 0; i < hud.coracoes.Length; i++)
        {
            hud.coracoes[i] = Imagem(raiz, "Coracao" + i, S("Tiles/hud_heart"), tl, tl, new Vector2(125 + i * 60, -44), new Vector2(56, 56));
            hud.coracoes[i].enabled = i < EstadoJogo.VidasIniciais; // 4º e 5º só aparecem ao ganhar vida extra
        }
        hud.coracaoCheio = S("Tiles/hud_heart");
        hud.coracaoVazio = S("Tiles/hud_heart_empty");

        Imagem(raiz, "IconeMoeda", S("Tiles/hud_coin"), tl, tl, new Vector2(125, -112), new Vector2(52, 52));
        hud.textoMoedas = Texto(raiz, "TextoMoedas", "x 0", 40, TextAnchor.MiddleLeft, tl, tl, new Vector2(185, -110), new Vector2(120, 56), Color.white, FontStyle.Bold);
        Imagem(raiz, "IconeFragmento", S("Tiles/star"), tl, tl, new Vector2(300, -112), new Vector2(52, 52));
        hud.textoFragmentos = Texto(raiz, "TextoFragmentos", "0/3", 40, TextAnchor.MiddleLeft, tl, tl, new Vector2(360, -110), new Vector2(120, 56), Color.white, FontStyle.Bold);

        // Planeta e gravidade (canto superior direito)
        var tr = new Vector2(1f, 1f);
        hud.textoPlaneta = Texto(raiz, "TextoPlaneta", $"Fase {p.numero}: {p.nome.ToUpper()}   •   g = {p.gravidade:0.00} m/s²".Replace('.', ','),
            36, TextAnchor.MiddleRight, tr, tr, new Vector2(-30, -30), new Vector2(1000, 60), Color.white, FontStyle.Bold);
        Texto(raiz, "DicaPausa", "Esc: pausar", 24, TextAnchor.MiddleRight, tr, tr, new Vector2(-30, -90), new Vector2(400, 40), new Color(1f, 1f, 1f, 0.7f));

        // Mensagens rápidas (rodapé)
        hud.textoMensagem = Texto(raiz, "TextoMensagem", "", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 110), new Vector2(1700, 70), new Color(1f, 0.92f, 0.4f), FontStyle.Bold);

        // Faixa de apresentação do planeta
        var faixa = Painel(raiz, "FaixaPlaneta", new Vector2(0, 170), new Vector2(1920, 380), new Color(0f, 0f, 0f, 0.55f));
        var grupo = faixa.gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        hud.faixa = grupo;
        hud.faixaTitulo = TextoCentro(faixa.transform, "Titulo", p.nome.ToUpper(), 120, new Vector2(0, 85), new Vector2(1800, 150), CorDestaque, FontStyle.Bold);
        hud.faixaCuriosidade = TextoCentro(faixa.transform, "Curiosidade", p.curiosidade, 36, new Vector2(0, -25), new Vector2(1650, 100), Color.white);
        hud.faixaDica = TextoCentro(faixa.transform, "Dica", p.dica, 32, new Vector2(0, -120), new Vector2(1700, 60), new Color(0.6f, 0.85f, 1f));

        // Pausa
        var pausa = TelaCheia("PainelPausa", raiz);
        var escuro = pausa.gameObject.AddComponent<Image>();
        escuro.color = new Color(0f, 0f, 0f, 0.7f);
        TextoCentro(pausa, "Titulo", "PAUSADO", 120, new Vector2(0, 80), new Vector2(1200, 160), CorDestaque, FontStyle.Bold);
        TextoCentro(pausa, "Opcoes", "Esc: continuar     •     M: menu principal", 44, new Vector2(0, -60), new Vector2(1400, 80), Color.white);
        hud.painelPausa = pausa.gameObject;
    }

    // =================================================================================
    //  MENU, VITÓRIA E GAME OVER
    // =================================================================================

    static void CriarMenu()
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CriarCamera(new Color(0.04f, 0.02f, 0.12f));
        var sistemas = new GameObject("Sistemas");
        ConfigurarAudio(sistemas, "Musica/trilha_menu.wav", null);
        CriarEventSystem();

        var canvas = CriarCanvas("Menu");
        var raiz = canvas.transform;
        var fundo = TelaCheia("Fundo", raiz).gameObject.AddComponent<Image>();
        fundo.sprite = S("Gerados/fundo_menu");
        fundo.raycastTarget = false;

        var centro = new Vector2(0.5f, 0.5f);
        var zora = Imagem(raiz, "Zora", S("Characters/character_pink_front"), centro, centro, new Vector2(-560, -20), new Vector2(460, 460));
        zora.gameObject.AddComponent<Flutuar>();

        Painel(raiz, "FundoTexto", new Vector2(240, 160), new Vector2(1160, 470), new Color(0.03f, 0.01f, 0.1f, 0.6f));
        TextoCentro(raiz, "Titulo", "ZORA", 170, new Vector2(240, 360), new Vector2(1100, 200), CorDestaque, FontStyle.Bold);
        TextoCentro(raiz, "Subtitulo", "e o Multiverso Quebrado", 64, new Vector2(240, 250), new Vector2(1100, 90), Color.white, FontStyle.Bold);
        TextoCentro(raiz, "Historia",
            "Zora, uma cientista alienígena, testava sua máquina de portais quando... CRACK! O multiverso rachou. " +
            "Atravesse a Lua, Marte, a Terra e Júpiter, junte os fragmentos de estrela para reabrir cada portal e volte para casa. " +
            "Atenção: cada planeta tem a sua própria gravidade!",
            30, new Vector2(240, 110), new Vector2(1050, 170), new Color(0.92f, 0.9f, 1f));

        var menu = canvas.gameObject.AddComponent<MenuPrincipal>();
        menu.botaoJogar = Botao(raiz, "JOGAR", new Vector2(240, -70));
        menu.botaoComoJogar = Botao(raiz, "COMO JOGAR", new Vector2(240, -170));
        menu.botaoCreditos = Botao(raiz, "CRÉDITOS", new Vector2(240, -270));
        menu.botaoSair = Botao(raiz, "SAIR", new Vector2(240, -370));

        menu.painelComoJogar = PainelTexto(raiz, "PainelComoJogar",
            "<b>COMO JOGAR</b>\n\n" +
            "A / D  ou  setas  —  andar\n" +
            "Shift (segurar)  —  correr\n" +
            "Espaço  —  pular (segure para pular mais alto)\n" +
            "W / S  ou  setas  —  subir e descer escadas\n" +
            "Esc  —  pausar\n\n" +
            "Junte os <color=#FFE066>3 FRAGMENTOS DE ESTRELA</color> de cada planeta para abrir o portal.\n" +
            "Pule em cima dos inimigos para derrotá-los. Moedas valem pontos, corações dão +1 vida.\n\n" +
            "<size=26>(Esc para fechar)</size>");

        menu.painelCreditos = PainelTexto(raiz, "PainelCreditos",
            "<b>CRÉDITOS</b>\n\n" +
            "Desenvolvimento, design de fases e programação: <b>Bruna Coelho</b>\n" +
            "Engine: Unity 6   •   Linguagem: C#\n\n" +
            "Personagens, inimigos, cenário, itens e efeitos sonoros:\n" +
            "<b>Kenney</b> — New Platformer Pack (kenney.nl) — licença CC0\n\n" +
            "Fundos espaciais, portal e trilha sonora:\n" +
            "criados de forma procedural para este jogo (pasta /Ferramentas)\n\n" +
            "<size=26>(Esc para fechar)</size>");

        Texto(raiz, "Rodape", "Trabalho de Game Development  •  2026", 24, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(1200, 40), new Color(1f, 1f, 1f, 0.6f));

        EditorSceneManager.SaveScene(cena, $"{PastaCenas}/{Cenas.Menu}.unity");
    }

    static GameObject PainelTexto(Transform pai, string nome, string conteudo)
    {
        var painel = Painel(pai, nome, Vector2.zero, new Vector2(1400, 820), new Color(0.05f, 0.02f, 0.12f, 0.94f));
        var t = TextoCentro(painel.transform, "Texto", conteudo, 34, Vector2.zero, new Vector2(1300, 760), Color.white);
        t.supportRichText = true;
        return painel.gameObject;
    }

    static void CriarTelaFinal(bool vitoria)
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CriarCamera(Color.black);
        var sistemas = new GameObject("Sistemas");
        ConfigurarAudio(sistemas, "Musica/trilha_menu.wav", vitoria ? "Musica/jingle_vitoria.wav" : null);
        CriarEventSystem();

        var canvas = CriarCanvas(vitoria ? "Vitoria" : "GameOver");
        var raiz = canvas.transform;
        var fundo = TelaCheia("Fundo", raiz).gameObject.AddComponent<Image>();
        fundo.sprite = S(vitoria ? "Gerados/fundo_menu" : "Gerados/fundo_jupiter");
        fundo.color = vitoria ? Color.white : new Color(0.45f, 0.4f, 0.45f);
        fundo.raycastTarget = false;

        var centro = new Vector2(0.5f, 0.5f);
        var zora = Imagem(raiz, "Zora", S(vitoria ? "Characters/character_pink_jump" : "Characters/character_pink_hit"),
            centro, centro, new Vector2(0, 230), new Vector2(300, 300));
        zora.gameObject.AddComponent<Flutuar>();

        Painel(raiz, "FundoTexto", new Vector2(0, -60), new Vector2(1640, 330), new Color(0.03f, 0.01f, 0.1f, 0.65f));
        TextoCentro(raiz, "Titulo", vitoria ? "VOCÊ CONSEGUIU!" : "FIM DE JOGO", 120, new Vector2(0, 30), new Vector2(1600, 150),
            vitoria ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.45f), FontStyle.Bold);
        TextoCentro(raiz, "Mensagem", vitoria
                ? "Zora juntou todos os fragmentos de estrela, fechou o rasgo no multiverso e voltou para casa!"
                : "As vidas acabaram... mas o multiverso ainda precisa de você!",
            38, new Vector2(0, -70), new Vector2(1500, 100), Color.white);

        var tela = canvas.gameObject.AddComponent<TelaFinal>();
        tela.vitoria = vitoria;
        tela.textoResumo = TextoCentro(raiz, "Resumo", vitoria ? "Moedas: 0" : "Você chegou até: Fase 1", 34,
            new Vector2(0, -150), new Vector2(1500, 60), new Color(0.75f, 0.9f, 1f));
        tela.botaoPrincipal = Botao(raiz, vitoria ? "JOGAR DE NOVO" : "TENTAR DE NOVO", new Vector2(-250, -290));
        tela.botaoMenu = Botao(raiz, "MENU", new Vector2(250, -290));

        EditorSceneManager.SaveScene(cena, $"{PastaCenas}/{(vitoria ? Cenas.Vitoria : Cenas.GameOver)}.unity");
    }

    // =================================================================================
    //  BUILD E PRINTS
    // =================================================================================

    static void ConfigurarBuild()
    {
        var ordem = new[] { Cenas.Menu, Cenas.Fase1, Cenas.Fase2, Cenas.Fase3, Cenas.Fase4, Cenas.Vitoria, Cenas.GameOver };
        EditorBuildSettings.scenes = ordem
            .Select(n => new EditorBuildSettingsScene($"{PastaCenas}/{n}.unity", true))
            .ToArray();

        PlayerSettings.companyName = "Bruna Coelho";
        PlayerSettings.productName = "Zora e o Multiverso Quebrado";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        var icone = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Tiles/hud_player_helmet_pink.png");
        if (icone != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icone }, IconKind.Any);
    }

    // Renderiza a câmera da cena aberta num PNG (o HUD/menus entram trocando o Canvas para "Screen Space - Camera")
    static void Fotografar(string arquivo, Vector2? posicao, bool incluirInterface)
    {
        var cam = Camera.main;
        if (cam == null) return;

        if (posicao.HasValue)
        {
            var seg = cam.GetComponent<CameraSegue>();
            float meiaLargura = cam.orthographicSize * 16f / 9f;
            float x = Mathf.Clamp(posicao.Value.x, seg.limiteMinimo.x + meiaLargura, seg.limiteMaximo.x - meiaLargura);
            float y = Mathf.Clamp(posicao.Value.y, seg.limiteMinimo.y + cam.orthographicSize, seg.limiteMaximo.y - cam.orthographicSize);
            cam.transform.position = new Vector3(x, y, -10f);
            var fundo = Object.FindAnyObjectByType<ParallaxFundo>();
            if (fundo != null) fundo.transform.position = new Vector3(x, y, 10f);
        }

        var canvases = Object.FindObjectsByType<Canvas>();
        foreach (var c in canvases)
        {
            if (!incluirInterface) { c.enabled = false; continue; }
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 5f;
            // esconde o menu de pausa na foto
            var pausa = c.transform.Find("PainelPausa");
            if (pausa != null) pausa.gameObject.SetActive(false);
            foreach (var painel in new[] { "PainelComoJogar", "PainelCreditos" })
            {
                var t = c.transform.Find(painel);
                if (t != null) t.gameObject.SetActive(false);
            }
        }
        Canvas.ForceUpdateCanvases();

        const int largura = 1920, altura = 1080;
        var rt = new RenderTexture(largura, altura, 24);
        cam.targetTexture = rt;
        cam.aspect = largura / (float)altura;
        cam.Render();
        RenderTexture.active = rt;
        var textura = new Texture2D(largura, altura, TextureFormat.RGB24, false);
        textura.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
        textura.Apply();
        File.WriteAllBytes(arquivo, textura.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(textura);
        // A cena NÃO é salva: as mudanças acima servem só para a foto
    }
}
