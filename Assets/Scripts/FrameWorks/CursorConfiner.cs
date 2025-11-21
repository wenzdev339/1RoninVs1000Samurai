using UnityEngine;

// Lock Mouse FullScreen
public class CursorConfiner : MonoBehaviour
{
    private static CursorConfiner instance;

    private void Awake()
    {
        // ตรวจสอบว่ามี Instance อยู่แล้วหรือไม่
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // ถ้ามีอยู่แล้ว ให้ทำลาย Instance ซ้ำ
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CheckAndConfineCursor();
    }

    private void Update()
    {
        // ตรวจสอบทุกเฟรมเผื่อมีการเปลี่ยนแปลง
        CheckAndConfineCursor();
    }

    private void CheckAndConfineCursor()
    {
        // ตรวจสอบว่าอยู่ในโหมด FullScreen หรือไม่
        if (Screen.fullScreen)
        {
            // ล็อคเมาส์ไม่ให้หลุดไปหน้าจออื่น
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            // ถ้าไม่ใช่ FullScreen ให้เมาส์เคลื่อนที่ได้ตามปกติ
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // ฟังก์ชันสำหรับเปลี่ยนโหมด
    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        CheckAndConfineCursor();
    }
}