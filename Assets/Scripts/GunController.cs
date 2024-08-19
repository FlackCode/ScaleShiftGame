using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    [Header("Gun Settings")]
    public float shootForce = 30f;
    public int maxAmmo = 7;
    public float reloadTime = 2f;
    public GameObject bulletObj;
    public Transform firePoint;

    [Header("Aiming")]
    public Camera playerCamera;
    public float aimSpeed = 5f;

    private int currentAmmo;
    private bool isReloading = false;
    // Start is called before the first frame update
    void Start()
    {
        currentAmmo = maxAmmo;
    }

    // Update is called once per frame
    void Update()
    {
        if (isReloading) return;

        if (Input.GetButtonDown("Fire1") && currentAmmo > 0) {
            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            StartCoroutine(Reload());
        }

        Aim();
    }

    private void Shoot() {
        GameObject bullet = Instantiate(bulletObj, firePoint.position, firePoint.rotation);
        Rigidbody rigidBody = bullet.GetComponent<Rigidbody>();
        rigidBody.AddForce(firePoint.forward * shootForce, ForceMode.Impulse);
        currentAmmo--;
    }

    private IEnumerator Reload() {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    private void Aim() {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            Vector3 targetPosition = hit.point;
            firePoint.LookAt(targetPosition);
        }
    }
}
