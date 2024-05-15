using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the opening and closing animations of a hamburger menu in a UI.
/// Provides functionality to animate the menu sliding in and out from the side, with buttons to control the visibility state of the menu.
/// </summary>
public class UIHamburger : MonoBehaviour
{
    [SerializeField] private GameObject BtnHamburgerMenu;
    [SerializeField] private GameObject HamburgerMenu;
    [SerializeField] private GameObject BtnCloseHamburgerMenu;

    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private float offscreenXOffset = -400f;

    private RectTransform hamburgerRect;
    private Vector2 initialPosition;
    private Vector2 targetPosition;

    /// <summary>
    /// Initializes the menu by setting its initial and target positions and managing the visibility of buttons and the menu itself.
    /// </summary>
    private void Start()
    {
        hamburgerRect = HamburgerMenu.GetComponent<RectTransform>();

        targetPosition = new Vector2(0, hamburgerRect.anchoredPosition.y);

        initialPosition = targetPosition;
        initialPosition.x = offscreenXOffset;

        // Set the initial off-screen position
        hamburgerRect.anchoredPosition = initialPosition;

        BtnHamburgerMenu.SetActive(true);
        HamburgerMenu.SetActive(true);
        BtnCloseHamburgerMenu.SetActive(false);
    }

    /// <summary>
    /// Opens the hamburger menu and triggers the animation from the offscreen to onscreen position.
    /// </summary>
    public void OpenHamburgerMenu()
    {
        BtnHamburgerMenu.SetActive(false);
        BtnCloseHamburgerMenu.SetActive(true);
        HamburgerMenu.SetActive(true);

        Debug.Log("Opening Hamburger Menu");

        StartCoroutine(AnimateMenu(initialPosition, targetPosition));
    }

    /// <summary>
    /// Closes the hamburger menu and triggers the animation from the onscreen to offscreen position.
    /// </summary>
    public void CloseHamburgerMenu()
    {
        BtnHamburgerMenu.SetActive(true);
        BtnCloseHamburgerMenu.SetActive(false);

        Debug.Log("Closing Hamburger Menu");

        StartCoroutine(AnimateMenu(targetPosition, initialPosition));
    }

    /// <summary>
    /// Animates the sliding movement of the hamburger menu.
    /// </summary>
    /// <param name="from">The starting position of the animation.</param>
    /// <param name="to">The ending position of the animation.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator AnimateMenu(Vector2 from, Vector2 to)
    {
        Debug.Log("From: " + from.ToString() + " To: " + to.ToString());
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            hamburgerRect.anchoredPosition = Vector2.Lerp(from, to, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        hamburgerRect.anchoredPosition = to;
    }

    /// <summary>
    /// Resets the menu to its initial position and state, setting the menu offscreen and resetting button visibility.
    /// </summary>
    public void Reset()
    {
        hamburgerRect = HamburgerMenu.GetComponent<RectTransform>();

        targetPosition = new Vector2(0, hamburgerRect.anchoredPosition.y);

        initialPosition = targetPosition;
        initialPosition.x = offscreenXOffset;

        hamburgerRect.anchoredPosition = initialPosition;

        BtnHamburgerMenu.SetActive(true);
        HamburgerMenu.SetActive(true);
        BtnCloseHamburgerMenu.SetActive(false);

        Debug.Log("Reset Hamburger Menu");
    }
}