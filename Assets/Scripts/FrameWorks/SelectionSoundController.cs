using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class SelectionSoundController : MonoBehaviour
{
    [Header("Navigation Sound")]
    public AudioSource audioSource;
    public AudioClip selectionChangedSound;
    [Range(0.01f, 1.0f)]
    public float navigationSoundVolume = 0.3f; // ระดับเสียงตอนเลื่อน
    
    [Header("Button Press Sound")]
    public AudioClip buttonPressSound; // เสียงตอนกดปุ่ม
    [Range(0.01f, 1.0f)]
    public float pressSoundVolume = 0.3f; // ระดับเสียงตอนกดปุ่ม
    
    private GameObject lastSelected;
    private bool isFirstFrame = true;
    
    void Start()
    {
        // บันทึกปุ่มที่ถูกเลือกเริ่มแรก แต่ไม่เล่นเสียง
        lastSelected = EventSystem.current.currentSelectedGameObject;
        
        // เพิ่ม listener ให้กับปุ่มทุกตัวในฉาก
        AddClickSoundToAllButtons();
    }
    
    void Update()
    {
        // ข้ามการเล่นเสียงในเฟรมแรก (auto-select)
        if (isFirstFrame)
        {
            isFirstFrame = false;
            return;
        }
        
        // ตรวจสอบว่า selection มีการเปลี่ยนแปลงหรือไม่
        if (EventSystem.current.currentSelectedGameObject != lastSelected && 
            EventSystem.current.currentSelectedGameObject != null)
        {
            // เล่นเสียงเมื่อมีการเปลี่ยน selection
            if (audioSource != null && selectionChangedSound != null)
            {
                // ตั้งระดับเสียงก่อนเล่น
                audioSource.volume = navigationSoundVolume;
                audioSource.PlayOneShot(selectionChangedSound);
            }
            
            // อัพเดทค่า lastSelected
            lastSelected = EventSystem.current.currentSelectedGameObject;
        }
    }
    
    // เพิ่ม listener ให้กับปุ่มทุกตัวที่มีในฉาก
    void AddClickSoundToAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            button.onClick.AddListener(() => PlayButtonPressSound());
        }
    }
    
    // เพิ่ม listener ให้กับปุ่มใหม่ที่อาจถูกสร้างหลังจากเริ่มเกม
    public void AddClickSoundToButton(Button button)
    {
        button.onClick.AddListener(() => PlayButtonPressSound());
    }
    
    // ฟังก์ชันเล่นเสียงเมื่อกดปุ่ม
    public void PlayButtonPressSound()
    {
        if (audioSource != null && buttonPressSound != null)
        {
            audioSource.volume = pressSoundVolume;
            audioSource.PlayOneShot(buttonPressSound);
        }
    }
}