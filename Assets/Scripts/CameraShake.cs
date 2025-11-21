using System.Collections;
using UnityEngine;
using Cinemachine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera cinemachineCamera;
    
    [Header("Shake Settings")]
    [SerializeField] private float shakeAmplitude = 2f;
    [SerializeField] private float shakeDuration = 0.2f;
    
    [Header("Pattern Settings")]
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private CinemachineBasicMultiChannelPerlin noise;
    private Coroutine currentShake;

    private void Awake()
    {
        if (cinemachineCamera != null)
        {
            noise = cinemachineCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    public void ShakeCamera()
    {
        if (noise == null) return;

        // หยุด shake เก่าถ้ามี
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        currentShake = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float strength = shakeCurve.Evaluate(elapsedTime / shakeDuration);
            
            noise.m_AmplitudeGain = shakeAmplitude * strength;

            yield return null;
        }

        // รีเซ็ตกลับเป็น 0
        noise.m_AmplitudeGain = 0f;
        currentShake = null;
    }

    private void OnDestroy()
    {
        // รีเซ็ตเมื่อถูกทำลาย
        if (noise != null)
        {
            noise.m_AmplitudeGain = 0f;
        }
    }
}