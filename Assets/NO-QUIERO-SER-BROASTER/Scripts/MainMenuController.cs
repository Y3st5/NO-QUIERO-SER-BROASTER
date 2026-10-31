using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string sceneToLoad = "Nivel1";

    private UIDocument uiDocument;
    private readonly Stack<VisualElement> menuStack = new Stack<VisualElement>();

    private VisualElement mainMenuContainer;
    private VisualElement optionsMenuContainer;

    // Botones
    private Button playButton;
    private Button optionsButton;
    private Button exitButton;
    private Button backButton;

    // Textos (Labels) a traducir
    private Label optionsTitle;
    private Label lblVolume;
    private Label lblFullscreen;
    private Label lblLanguage;

    // Controles
    private Slider volumeSlider;
    private Toggle fullscreenToggle;
    private DropdownField languageDropdown;

    // Idiomas reales del proyecto, en el mismo orden que las opciones del desplegable
    private readonly List<Locale> idiomas = new List<Locale>();
    private readonly List<string> nombresIdiomas = new List<string>();

    // Callbacks guardados en variables para poder desuscribirlos (los lambdas anónimos no se pueden quitar)
    private Action onPlayClicked;
    private Action onOptionsClicked;
    private Action onExitClicked;
    private Action onBackClicked;
    private EventCallback<ChangeEvent<float>> onVolumeChanged;
    private EventCallback<ChangeEvent<bool>> onFullscreenChanged;
    private EventCallback<ChangeEvent<string>> onLanguageChanged;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        onPlayClicked = OnPlayClicked;
        onOptionsClicked = () => PushMenu(optionsMenuContainer);
        onExitClicked = OnExitClicked;
        onBackClicked = PopMenu;

        onVolumeChanged = evt => AudioListener.volume = Mathf.Clamp01(evt.newValue / 100f);
        onFullscreenChanged = evt => Screen.fullScreen = evt.newValue;
        onLanguageChanged = evt => CambiarIdioma(evt.newValue);
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        RellenarSelectorIdioma();
        TraducirTextosUI();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnEnable()
    {
        if (uiDocument == null) return;

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null) return;

        mainMenuContainer = root.Q<VisualElement>("MainContainer");
        optionsMenuContainer = root.Q<VisualElement>("OptionsMenuContainer");

        // Reiniciamos la pila de menús para que no se acumulen elementos si el menú se reactiva
        menuStack.Clear();
        if (optionsMenuContainer != null) optionsMenuContainer.style.display = DisplayStyle.None;
        if (mainMenuContainer != null)
        {
            mainMenuContainer.style.display = DisplayStyle.Flex;
            menuStack.Push(mainMenuContainer);
        }

        // Capturar Botones
        playButton = root.Q<Button>("BPlay");
        optionsButton = root.Q<Button>("BOptions");
        exitButton = root.Q<Button>("BExit");
        backButton = root.Q<Button>("BBack");

        // Capturar Textos
        optionsTitle = root.Q<Label>("OptionsTitle");
        lblVolume = root.Q<Label>("LblVolume");
        lblFullscreen = root.Q<Label>("LblFullscreen");
        lblLanguage = root.Q<Label>("LblLanguage");

        // Capturar Controles
        volumeSlider = root.Q<Slider>("VolumeSlider");
        fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        languageDropdown = root.Q<DropdownField>("LanguageDropdown");

        // Eventos de botones
        if (playButton != null) playButton.clicked += onPlayClicked;
        if (exitButton != null) exitButton.clicked += onExitClicked;
        if (optionsButton != null) optionsButton.clicked += onOptionsClicked;
        if (backButton != null) backButton.clicked += onBackClicked;

        // Eventos de controles
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume * 100f);
            volumeSlider.RegisterValueChangedCallback(onVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetValueWithoutNotify(Screen.fullScreen);
            fullscreenToggle.RegisterValueChangedCallback(onFullscreenChanged);
        }

        if (languageDropdown != null)
        {
            // Si los idiomas ya se cargaron (Start ya pasó), reaplicamos la lista
            AplicarIdiomasAlDropdown();
            languageDropdown.RegisterValueChangedCallback(onLanguageChanged);
        }
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.clicked -= onPlayClicked;
        if (exitButton != null) exitButton.clicked -= onExitClicked;
        if (optionsButton != null) optionsButton.clicked -= onOptionsClicked;
        if (backButton != null) backButton.clicked -= onBackClicked;

        if (volumeSlider != null) volumeSlider.UnregisterValueChangedCallback(onVolumeChanged);
        if (fullscreenToggle != null) fullscreenToggle.UnregisterValueChangedCallback(onFullscreenChanged);
        if (languageDropdown != null) languageDropdown.UnregisterValueChangedCallback(onLanguageChanged);

        menuStack.Clear();
    }

    // ---------------- IDIOMAS ----------------

    private void OnLocaleChanged(Locale locale)
    {
        TraducirTextosUI();
        AplicarIdiomasAlDropdown();
    }

    /// <summary>
    /// Lee los idiomas configurados en Localization y los pone como opciones del desplegable.
    /// Así no hay que hardcodear nombres ni códigos en el script ni en el UXML.
    /// </summary>
    private void RellenarSelectorIdioma()
    {
        idiomas.Clear();
        nombresIdiomas.Clear();

        ILocalesProvider proveedor = LocalizationSettings.AvailableLocales;
        if (proveedor == null || proveedor.Locales == null)
            return;

        IList<Locale> disponibles = proveedor.Locales;

        for (int i = 0; i < disponibles.Count; i++)
        {
            Locale locale = disponibles[i];
            if (locale == null) continue;

            string codigo = locale.Identifier.Code;
            string nombre = string.IsNullOrEmpty(locale.LocaleName) ? codigo : locale.LocaleName;

            // Si dos idiomas se llaman igual (p. ej. dos variantes de inglés), añadimos el código.
            if (nombresIdiomas.Contains(nombre))
                nombre += " (" + codigo + ")";

            idiomas.Add(locale);
            nombresIdiomas.Add(nombre);
        }

        AplicarIdiomasAlDropdown();
    }

    private void AplicarIdiomasAlDropdown()
    {
        if (languageDropdown == null || nombresIdiomas.Count == 0)
            return;

        int indice = idiomas.IndexOf(LocalizationSettings.SelectedLocale);
        if (indice < 0) indice = 0;

        languageDropdown.choices = new List<string>(nombresIdiomas);
        languageDropdown.SetValueWithoutNotify(nombresIdiomas[indice]);
        languageDropdown.index = indice;
    }

    private void CambiarIdioma(string idiomaSeleccionado)
    {
        int indice = nombresIdiomas.IndexOf(idiomaSeleccionado);
        if (indice < 0 || indice >= idiomas.Count)
            return;

        Locale destino = idiomas[indice];
        if (destino == null || LocalizationSettings.SelectedLocale == destino)
            return;

        LocalizationSettings.SelectedLocale = destino;
    }

    // ---------------- TEXTOS ----------------

    private void TraducirTextosUI()
    {
        // Traducir Botones
        if (playButton != null) playButton.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "btn_play");
        if (optionsButton != null) optionsButton.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "btn_options");
        if (exitButton != null) exitButton.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "btn_exit");
        if (backButton != null) backButton.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "btn_back");

        // Traducir Textos (Labels)
        if (optionsTitle != null) optionsTitle.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "lbl_title_options");
        if (lblVolume != null) lblVolume.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "lbl_volume");
        if (lblFullscreen != null) lblFullscreen.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "lbl_fullscreen");
        if (lblLanguage != null) lblLanguage.text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuTextos", "lbl_language");
    }

    // ---------------- NAVEGACION ----------------

    private void PushMenu(VisualElement newMenu)
    {
        if (newMenu == null) return;

        if (menuStack.Count > 0) menuStack.Peek().style.display = DisplayStyle.None;
        menuStack.Push(newMenu);
        newMenu.style.display = DisplayStyle.Flex;
    }

    private void PopMenu()
    {
        if (menuStack.Count > 1)
        {
            menuStack.Pop().style.display = DisplayStyle.None;
            menuStack.Peek().style.display = DisplayStyle.Flex;
        }
    }

    private void OnPlayClicked()
    {
        if (!string.IsNullOrEmpty(sceneToLoad)) SceneManager.LoadScene(sceneToLoad);
    }

    private void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
