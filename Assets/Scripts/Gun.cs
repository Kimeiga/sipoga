using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class Gun : MonoBehaviour
{
    //references
    private Item itemScript;
    private AudioSource fireAudioSource;
    public Transform fireTransform;
    public MouseLook fireTransformRotate;
    public MouseLook bodyTransformRotate;
    public Sway handsSway;


    //gun stats
    public int maxAmmo;
    private int ammo;

    public int Ammo
    {
        get { return ammo; }

        set
        {
            ammo = value;

            if (inventoryScript)
            {
                //inventoryScript.ammoText.text = ammo.ToString();
            }
        }
    }

    public bool automatic;
    private bool fireCommand;
    public bool firing = false;
    public bool firingAux = false;

    public float fireRate = 0.02f;
    private float nextFire;

    public float range = 1000;

    public float damage = 20;


    public float scaleMod = 3;
    public float minScaleMod = 0.5f;
    public float maxScaleMod = 3;

    //firing variables
    private RaycastHit hit;
    public LayerMask fireMask;
    public GameObject bullet;


    //we're going to do automatic fire first ok?
    // public AnimationCurve xRecoil;
    // public AnimationCurve yRecoil;
    public float xRecoil;
    public float yRecoil;

    public float currentRecoil;
    public float currentRecoilIncrement;

    public float recoilModX = 3;
    public float recoilModY = 3;

    public float xKick;
    public float yKick;

    public float kickTween;

    public float kickMod = 2;

    public float xRecoilOffset; //smooth recoil pattern from the whole spray
    public float yRecoilOffset;

    public float recoilTween;

    public float xKickOffset; //oneshot kicks from each bullet
    public float yKickOffset;

    public AnimationCurve kickIntensity;

    public float kickback;
    public float kickbackAcc;


    public Inventory inventoryScript;


    private Tweener[] tweeners;
    private Tweener tempTweener;

    // Use this for initialization
    void Start()
    {
        //initialize references
        itemScript = GetComponent<Item>();
        fireAudioSource = GetComponent<AudioSource>();


        //initialize numbers
        nextFire = 0;
        Ammo = maxAmmo;


        currentRecoil = 0;

        //Calculate current recoil increment
        currentRecoilIncrement = (1.0f / maxAmmo);
    }

    // Update is called once per frame
    void Update()
    {
        if (itemScript.active)
        {
            //cue fire command depending on the the automatic fire bool
            if (automatic)
            {
                fireCommand = Input.GetButton("Fire");
            }

            if (!automatic)
            {
                fireCommand = Input.GetButtonDown("Fire");
            }


            currentRecoil = Mathf.Clamp(currentRecoil, 0, 1);


            if (fireCommand)
            {
                if (Ammo > 0)
                {
                    firing = true;
                    firingAux = true;

                    if (Time.time > nextFire)
                    {
                        Vector3 recoilOffset = new Vector3(xRecoilOffset * 0.5f, yRecoilOffset * 0.5f, 0);


                        Quaternion xQuaternion = Quaternion.AngleAxis(recoilOffset.x, Vector3.up);
                        Quaternion yQuaternion = Quaternion.AngleAxis(recoilOffset.y, -Vector3.right);

                        Quaternion shotDirection = fireTransform.rotation * xQuaternion * yQuaternion;


                        //fire
                        if (Physics.Raycast(fireTransform.position, shotDirection * Vector3.forward, out hit, range,
                            fireMask))
                        {
                            //you can use this for reflecting guns soon! :D
                            //Vector3 incomingVec = hit.point - transform.position;
                            //Vector3 reflectVec = Vector3.Reflect(incomingVec, hit.normal);
                            //Debug.DrawLine(transform.position, hit.point, Color.red);
                            //Debug.DrawRay(hit.point, reflectVec, Color.green);


                            Quaternion rot = Quaternion.LookRotation(-fireTransform.forward);

                            GameObject bul = (GameObject) Instantiate(bullet, hit.point, Quaternion.identity);

                            // This is for the bullet decals
                            // we will download a decal solution later, like the paid
                            // one that you found on peoples' githubs >:)
                            // Bullet bulScript = bul.GetComponent<Bullet>();
                            //
                            // bul.transform.SetParent(hit.transform);
                            //
                            // Vector3 temp = hit.transform.localScale;
                            // temp.x = 1 / temp.x;
                            // temp.y = 1 / temp.y;
                            // temp.z = 1 / temp.z;
                            //
                            // bul.transform.localScale = temp;
                            //
                            // bulScript.bulletActual.transform.localRotation = rot;
                            //
                            //
                            // float bulletScaleMod = hit.distance * scaleMod;
                            // bulletScaleMod = Mathf.Clamp(bulletScaleMod, minScaleMod, maxScaleMod);
                            //
                            //
                            // bulScript.bulletActual.transform.localScale *= bulletScaleMod;
                        }

                        nextFire = Time.time + fireRate;

                        //fireAudioSource.PlayOneShot(fireAudioSource.clip);

                        Ammo--;

                        currentRecoil += currentRecoilIncrement;

                        foreach (Tweener tweener in tweeners)
                        {
                            DOTween.Kill(tweener);
                        }
                        // DOTween.Kill();
                        // LeanTween.cancel(gameObject);

                        xRecoilOffset += xRecoil * recoilModX;
                        yRecoilOffset += yRecoil * recoilModY;

                        Vector3 kickDir = new Vector3(xRecoilOffset, yRecoilOffset, 0);
                        kickDir = kickDir.normalized;
                        kickDir *= kickMod;
                        kickDir *= kickIntensity.Evaluate(currentRecoil);

                        xKick = kickDir.x;
                        yKick = kickDir.y;

                        kickbackAcc -= kickback;
                    }
                }
                else
                {
                    //gun is empty
                    //play empty sound
                    firing = false;
                }
            }
            else
            {
                firing = false;
            }


            if (firing == false && firingAux == true)
            {
                currentRecoil = 0;

                tempTweener = DOTween.To(() => yRecoilOffset, x => yRecoilOffset = x, 0, recoilTween).SetEase(Ease.OutQuart);
                tweeners.Append(tempTweener);
                tempTweener = DOTween.To(() => xRecoilOffset, x => xRecoilOffset = x, 0, recoilTween).SetEase(Ease.OutQuart);
                tweeners.Append(tempTweener);
                // LeanTween.value(gameObject, yRecoilOffset, 0, recoilTween).setEase(LeanTweenType.easeOutQuart)
                //     .setOnUpdate((float val) => { yRecoilOffset = val; });
                // LeanTween.value(gameObject, xRecoilOffset, 0, recoilTween).setEase(LeanTweenType.easeOutQuart)
                //     .setOnUpdate((float val) => { xRecoilOffset = val; });

                firingAux = false;
            }


            bodyTransformRotate.xOffset = (xRecoilOffset / 2) + xKick;
            fireTransformRotate.yOffset = (yRecoilOffset / 2) + yKick;

            
            // lookTransformRotate.xOffset = xKick;
            // lookTransformRotate.yOffset = yKick;


            handsSway.zOffset = kickbackAcc;


            tempTweener = DOTween.To(() => xKick, x => xKick = x, 0, kickTween);
            tweeners.Append(tempTweener);
            
            tempTweener = DOTween.To(() => yKick, x => yKick = x, 0, kickTween);
            tweeners.Append(tempTweener);

            tempTweener = DOTween.To(() => kickbackAcc, x => kickbackAcc = x, 0, kickTween);
            tweeners.Append(tempTweener);
            // LeanTween.value(gameObject, xKick, 0, kickTween).setOnUpdate((float val) => { xKick = val; });
            // LeanTween.value(gameObject, yKick, 0, kickTween).setOnUpdate((float val) => { yKick = val; });
            //
            // LeanTween.value(gameObject, kickbackAcc, 0, kickTween).setOnUpdate((float val) => { kickbackAcc = val; });


            if (Input.GetButtonDown("Reload"))
            {
                Ammo = maxAmmo;
            }
        }
    }
}