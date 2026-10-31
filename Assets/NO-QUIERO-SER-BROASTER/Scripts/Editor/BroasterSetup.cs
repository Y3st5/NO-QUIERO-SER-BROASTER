#if UNITY_EDITOR
using System.IO;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Broaster.EditorTools
{
    /// <summary>
    /// Configura de una sola pasada todo lo que necesita el lanzamiento del Chef:
    /// materiales, prefabs del proyectil y del charco, feedback de daño (Feel + URP)
    /// y el cableado de la escena.
    /// Es idempotente: solo crea lo que falta.
    /// </summary>
    public static class BroasterSetup
    {
        public const string MenuPath = "Tools/Broaster/Configurar lanzamiento y feedback";
        private const string MenuForzar = "Tools/Broaster/Forzar reconfiguracion";

        private const string Raiz = "Assets/NO-QUIERO-SER-BROASTER";
        private const string CarpetaPrefabs = Raiz + "/Prefabs";
        private const string CarpetaMateriales = Raiz + "/Materiales";
        private const string RutaProyectil = CarpetaPrefabs + "/ProyectilBroaster.prefab";
        private const string RutaCharco = CarpetaPrefabs + "/Charco.prefab";
        private const string RutaPaloma = CarpetaPrefabs + "/Paloma.prefab";
        private const string RutaPerfil = "Assets/Settings/DamageVolumeProfile.asset";
        private const string RutaEscena = "Assets/Scenes/NO-QUIERO-SER-BROASTER.unity";
        private const string NombreVolumen = "DamageVolume";

        [MenuItem(MenuPath)]
        public static void ConfigurarTodo()
        {
            Ejecutar(abrirEscenaSiHaceFalta: true, verboso: true);
        }

        [MenuItem(MenuForzar)]
        public static void ForzarReconfiguracion()
        {
            EditorPrefs.DeleteKey(BroasterSetupAutoRun.ClaveAutoRun);
            Ejecutar(abrirEscenaSiHaceFalta: true, verboso: true);
        }

        /// <summary>
        /// Punto de entrada usado tanto por el menú como por el autorun.
        /// </summary>
        public static void Ejecutar(bool abrirEscenaSiHaceFalta, bool verboso)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (verboso) Debug.LogWarning("[Broaster] Sal del modo Play antes de configurar.");
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                if (verboso) Debug.LogWarning("[Broaster] Cierra el modo Prefab antes de configurar.");
                return;
            }

            AsegurarCarpeta(CarpetaPrefabs);
            AsegurarCarpeta(CarpetaMateriales);

            Material materialProyectil = CrearMaterial("M_Proyectil", new Color(0.85f, 0.14f, 0.10f), 0.55f);
            Material materialCharco = CrearMaterial("M_Charco", new Color(0.13f, 0.09f, 0.05f), 0.95f);

            GameObject charcoPrefab = CrearPrefabCharco(materialCharco);
            GameObject proyectilPrefab = CrearPrefabProyectil(materialProyectil, charcoPrefab);

            ConfigurarPaloma();
            ConfigurarEscena(proyectilPrefab, abrirEscenaSiHaceFalta, verboso);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verboso)
                Debug.Log("[Broaster] Configuración lista: proyectil, charco, vidas y feedback de daño.");
        }

        // ---------------------------------------------------------------- MATERIALES

        private static Material CrearMaterial(string nombre, Color color, float suavidad)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
                return existente;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.name = nombre;

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", suavidad);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", suavidad);

            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        // ---------------------------------------------------------------- PREFABS

        private static GameObject CrearPrefabCharco(Material material)
        {
            GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCharco);
            if (existente != null)
                return existente;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Charco";

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            go.transform.localScale = new Vector3(2.4f, 0.012f, 2.4f);

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            go.AddComponent<Charco>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, RutaCharco);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabProyectil(Material material, GameObject charcoPrefab)
        {
            GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaProyectil);
            if (existente != null)
            {
                ProyectilChef scriptExistente = existente.GetComponent<ProyectilChef>();
                if (scriptExistente != null)
                {
                    SerializedObject so = new SerializedObject(scriptExistente);
                    so.FindProperty("charcoPrefab").objectReferenceValue = charcoPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                return existente;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ProyectilBroaster";
            go.transform.localScale = Vector3.one * 0.5f;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            Rigidbody cuerpo = go.AddComponent<Rigidbody>();
            cuerpo.mass = 1f;
            cuerpo.linearDamping = 0f;
            cuerpo.angularDamping = 0.05f;
            cuerpo.useGravity = true;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            cuerpo.interpolation = RigidbodyInterpolation.Interpolate;

            ProyectilChef script = go.AddComponent<ProyectilChef>();

            SerializedObject serializado = new SerializedObject(script);
            SerializedProperty propCharco = serializado.FindProperty("charcoPrefab");
            if (propCharco != null) propCharco.objectReferenceValue = charcoPrefab;
            propCharco = serializado.FindProperty("danio");
            if (propCharco != null) propCharco.intValue = 1;
            serializado.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, RutaProyectil);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------------------------------------------------------------- PALOMA

        private static void ConfigurarPaloma()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RutaPaloma);
            if (root == null)
            {
                Debug.LogWarning("[Broaster] No encontré el prefab de la Paloma en " + RutaPaloma + ".");
                return;
            }

            try
            {
                Paloma vida = root.GetComponent<Paloma>();
                if (vida == null) vida = root.AddComponent<Paloma>();

                MMF_Player feedbacks = root.GetComponent<MMF_Player>();
                if (feedbacks == null) feedbacks = root.AddComponent<MMF_Player>();

                ConfigurarFeedbacks(feedbacks);

                SerializedObject soVida = new SerializedObject(vida);
                SerializedProperty propFeedback = soVida.FindProperty("damageFeedback");
                if (propFeedback != null) propFeedback.objectReferenceValue = feedbacks;

                SerializedProperty propMax = soVida.FindProperty("vidasMax");
                if (propMax != null && propMax.intValue <= 0) propMax.intValue = 3;

                soVida.ApplyModifiedPropertiesWithoutUndo();

                Camera camara = root.GetComponentInChildren<Camera>(true);
                if (camara != null)
                    ConfigurarCamara(camara.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, RutaPaloma);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigurarCamara(GameObject camara)
        {
            MMWiggle wiggle = camara.GetComponent<MMWiggle>();
            if (wiggle == null) wiggle = camara.AddComponent<MMWiggle>();

            if (wiggle.PositionWiggleProperties == null)
                wiggle.PositionWiggleProperties = new WiggleProperties();

            // Solo se mueve durante un shake: nunca vibra sola ni de forma permanente.
            wiggle.PositionWiggleProperties.StartWigglingAutomatically = false;
            wiggle.PositionWiggleProperties.WigglePermitted = true;
            wiggle.PositionActive = true;

            if (camara.GetComponent<MMCameraShaker>() == null)
                camara.AddComponent<MMCameraShaker>();
        }

        private static void ConfigurarFeedbacks(MMF_Player feedbacks)
        {
            // Si ya tiene feedbacks configurados no se tocan (por si los ajustaste a mano).
            if (feedbacks.FeedbacksList != null && feedbacks.FeedbacksList.Count > 0)
                return;

            MMF_ColorAdjustments_URP parpadeo = new MMF_ColorAdjustments_URP
            {
                Label = "Parpadeo rojo",
                ShakeDuration = 0.35f,
                RelativeIntensity = false,
                ColorFilterMode = MMColorAdjustmentsShaker_URP.ColorFilterModes.Gradient,
                ColorFilterGradient = CrearGradienteRojo(),

                // El resto de parámetros se anulan para que SOLO actúe el color.
                ShakePostExposure = AnimationCurve.Constant(0f, 1f, 0f),
                RemapPostExposureZero = 0f,
                RemapPostExposureOne = 0f,

                ShakeHueShift = AnimationCurve.Constant(0f, 1f, 0f),
                RemapHueShiftZero = 0f,
                RemapHueShiftOne = 0f,

                ShakeSaturation = AnimationCurve.Constant(0f, 1f, 0f),
                RemapSaturationZero = 0f,
                RemapSaturationOne = 0f,

                ShakeContrast = AnimationCurve.Constant(0f, 1f, 0f),
                RemapContrastZero = 0f,
                RemapContrastOne = 0f,
            };

            feedbacks.AddFeedback(parpadeo);

            MMF_CameraShake sacudida = new MMF_CameraShake
            {
                Label = "Sacudida de camara",
                CameraShakeProperties = new MMCameraShakeProperties(0.25f, 0.12f, 40f),
            };

            feedbacks.AddFeedback(sacudida);

            EditorUtility.SetDirty(feedbacks);
        }

        private static Gradient CrearGradienteRojo()
        {
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.12f, 0.06f), 0.35f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });

            return gradiente;
        }

        // ---------------------------------------------------------------- ESCENA

        private static void ConfigurarEscena(GameObject proyectilPrefab, bool abrirEscenaSiHaceFalta, bool verboso)
        {
            Scene escena = SceneManager.GetActiveScene();

            if (escena.path != RutaEscena)
            {
                if (!abrirEscenaSiHaceFalta)
                {
                    if (verboso)
                        Debug.LogWarning("[Broaster] La escena activa no es NO-QUIERO-SER-BROASTER. " +
                                         "Abrela y usa " + MenuPath + " para cablear el Chef.");
                    return;
                }

                escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }

            ConfigurarVolumenDanio();
            ConfigurarChef(proyectilPrefab);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        private static void ConfigurarVolumenDanio()
        {
            GameObject go = GameObject.Find(NombreVolumen);
            if (go == null) go = new GameObject(NombreVolumen);

            Volume volumen = go.GetComponent<Volume>();
            if (volumen == null) volumen = go.AddComponent<Volume>();

            volumen.isGlobal = true;
            volumen.priority = 10f;
            volumen.weight = 1f;
            volumen.sharedProfile = CrearPerfilDanio();

            // RequireComponent(Volume) ya lo garantiza, pero comprobamos por seguridad.
            if (go.GetComponent<MMColorAdjustmentsShaker_URP>() == null)
                go.AddComponent<MMColorAdjustmentsShaker_URP>();
        }

        private static VolumeProfile CrearPerfilDanio()
        {
            VolumeProfile existente = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfil);
            if (existente != null)
                return existente;

            VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            perfil.name = "DamageVolumeProfile";

            AsegurarCarpeta(Path.GetDirectoryName(RutaPerfil).Replace('\\', '/'));
            AssetDatabase.CreateAsset(perfil, RutaPerfil);

            if (!perfil.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments = perfil.Add<ColorAdjustments>(true);
                colorAdjustments.colorFilter.Override(Color.white);
                AssetDatabase.AddObjectToAsset(colorAdjustments, perfil);
            }

            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfil);
        }

        private static void ConfigurarChef(GameObject proyectilPrefab)
        {
            ChefAI chef = Object.FindAnyObjectByType<ChefAI>();
            if (chef == null)
            {
                Debug.LogWarning("[Broaster] No encontré ningún ChefAI en la escena.");
                return;
            }

            SerializedObject so = new SerializedObject(chef);
            SerializedProperty prop = so.FindProperty("proyectilPrefab");
            if (prop != null) prop.objectReferenceValue = proyectilPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(chef);
        }

        // ---------------------------------------------------------------- UTILIDADES

        private static void AsegurarCarpeta(string ruta)
        {
            if (string.IsNullOrEmpty(ruta) || AssetDatabase.IsValidFolder(ruta))
                return;

            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            string nombre = Path.GetFileName(ruta);

            AsegurarCarpeta(padre);
            AssetDatabase.CreateFolder(padre, nombre);
        }
    }

    /// <summary>
    /// Ejecuta el setup una única vez, la primera vez que Unity compila estos scripts.
    /// Si algo falla, el menú "Tools/Broaster/Forzar reconfiguracion" lo reintenta.
    /// </summary>
    [InitializeOnLoad]
    internal static class BroasterSetupAutoRun
    {
        public const string ClaveAutoRun = "Broaster.SetupAuto.v1";

        static BroasterSetupAutoRun()
        {
            EditorApplication.delayCall += Intento;
        }

        private static void Intento()
        {
            if (EditorPrefs.GetBool(ClaveAutoRun, false))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Intento;
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                return;

            // Se marca antes de ejecutar para no entrar en bucle si falla.
            EditorPrefs.SetBool(ClaveAutoRun, true);

            try
            {
                BroasterSetup.Ejecutar(abrirEscenaSiHaceFalta: false, verboso: false);
                Debug.Log("[Broaster] Setup automático aplicado. Menú: " + BroasterSetup.MenuPath);
            }
            catch (System.Exception excepcion)
            {
                Debug.LogError("[Broaster] El setup automático falló: " + excepcion);
            }
        }
    }
}
#endif
