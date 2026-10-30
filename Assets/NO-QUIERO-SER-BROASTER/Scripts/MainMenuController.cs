using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string sceneToLoad = "Nivel1";

    private UIDocument uiDocument;
    private Stack<VisualElement> menuStack = new Stack<VisualElement>();

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

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        TraducirTextosUI();
        LocalizationSettings.SelectedLocaleChanged += (locale) => TraducirTextosUI();
    }

    private void OnEnable()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        mainMenuContainer = root.Q<VisualElement>("MainContainer");
        optionsMenuContainer = root.Q<VisualElement>("OptionsMenuContainer");

        optionsMenuContainer.style.display = DisplayStyle.None;
        PushMenu(mainMenuContainer);

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
        if (playButton != null) playButton.clicked += OnPlayClicked;
        if (exitButton != null) exitButton.clicked += OnExitClicked;
        if (optionsButton != null) optionsButton.clicked += () => PushMenu(optionsMenuContainer);
        if (backButton != null) backButton.clicked += PopMenu;

        // Eventos de controles
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume * 100f;
            volumeSlider.RegisterValueChangedCallback(evt => AudioListener.volume = evt.newValue / 100f);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.value = Screen.fullScreen;
            fullscreenToggle.RegisterValueChangedCallback(evt => Screen.fullScreen = evt.newValue);
        }

        if (languageDropdown != null)
        {
            languageDropdown.RegisterValueChangedCallback(evt => CambiarIdioma(evt.newValue));
        }
    }

    private void CambiarIdioma(string idiomaSeleccionado)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;

        for (int i = 0; i < locales.Count; i++)
        {
            string codigo = locales[i].Identifier.Code;

            if (idiomaSeleccionado == "Español" && codigo.StartsWith("es"))
                LocalizationSettings.SelectedLocale = locales[i];
            else if (idiomaSeleccionado == "English" && codigo.StartsWith("en"))
                LocalizationSettings.SelectedLocale = locales[i];
            else if (idiomaSeleccionado == "Português" && codigo.StartsWith("pt"))
                LocalizationSettings.SelectedLocale = locales[i];
        }
    }

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

    private void PushMenu(VisualElement newMenu)
    {
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