using System;
using UnityEngine;
using DG.Tweening;

public class SimpleGun : MonoBehaviour
{
	/// <summary>
	/// This gun has no recoil and minimal animations via sway. This is because the AI don't know how to compensate for
	/// recoil yet, so they should just use guns like this for now. Of course this gun can also be picked up by any player.
	/// </summary>

	//references
	private Item itemScript;

	private AudioSource fireAudioSource;
	public Transform fireTransform;
	public Sway handsSway;
	public Inventory inventoryScript;

	//gun stats
	public int maxAmmo;
	public int ammo;

	public int Ammo
	{
		get { return ammo; }

		set
		{
			ammo = value;

			HUDManager.Instance.ammoText.text = ammo.ToString();
		}
	}

	public bool automatic;
	private bool fireCommand;

	public float fireRate = 0.02f;
	private float nextFire;

	public float range = 1000;


	public float scaleMod = 3;
	public float minScaleMod = 0.5f;
	public float maxScaleMod = 3;

	//firing variables
	private RaycastHit hit;
	public LayerMask fireMask;
	public GameObject bullet;

	public float kickback;
	public float kickbackAcc;

	// Use this for initialization
	void Start()
	{
		//initialize references
		itemScript = GetComponent<Item>();
		fireAudioSource = GetComponent<AudioSource>();

		//initialize numbers
		nextFire = 0;
		Ammo = maxAmmo;
	}

	// Update is called once per frame
	void Update()
	{
		if (itemScript.active)
		{
			//cue fire command depending on the the automatic fire bool
			if (automatic)
			{
				fireCommand = Input.GetButton("Fire1");
			}

			if (!automatic)
			{
				fireCommand = Input.GetButtonDown("Fire1");
			}


			if (fireCommand)
			{
				if (Ammo > 0)
				{
					if (Time.time > nextFire)
					{
						Quaternion shotDirection = fireTransform.rotation;


						// fire
						// TODO: prevent you from shooting yourself by adding yourself to the mask somehow (?)
						// currently if you look straight down, you can shoot your torso...
						if (Physics.Raycast(fireTransform.position, shotDirection * Vector3.forward, out hit, range,
							fireMask))
						{
							//you can use this for reflecting guns soon! :D
							//Vector3 incomingVec = hit.point - transform.position;
							//Vector3 reflectVec = Vector3.Reflect(incomingVec, hit.normal);
							Debug.DrawLine(transform.position, hit.point, Color.red);
							//Debug.DrawRay(hit.point, reflectVec, Color.green);


							Quaternion rot = Quaternion.LookRotation(-fireTransform.forward);

							GameObject bul = Instantiate(bullet, hit.point, Quaternion.identity);

							// This is for the bullet decals
							// we will download a decal solution later, like the paid
							// one that you found on peoples' githubs >:)
							Bullet bulScript = bul.GetComponent<Bullet>();

							bul.transform.SetParent(hit.transform);

							Vector3 temp = hit.transform.localScale;
							temp.x = 1 / temp.x;
							temp.y = 1 / temp.y;
							temp.z = 1 / temp.z;

							bul.transform.localScale = temp;

							bulScript.bulletActual.transform.localRotation = rot;


							float bulletScaleMod = hit.distance * scaleMod;
							bulletScaleMod = Mathf.Clamp(bulletScaleMod, minScaleMod, maxScaleMod);


							bulScript.bulletActual.transform.localScale *= bulletScaleMod;
						}

						nextFire = Time.time + fireRate;

						fireAudioSource.PlayOneShot(fireAudioSource.clip);

						Ammo--;

						kickbackAcc -= kickback;
					}
				}
			}


			handsSway.zOffset = kickbackAcc;

			kickbackAcc -= 10 * Time.deltaTime;
			if (kickbackAcc < 0) kickbackAcc = 0;


			if (Input.GetButtonDown("Reload"))
			{
				Ammo = maxAmmo;
			}
		}
	}
}