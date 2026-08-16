using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using System.Collections;
using DG.Tweening;
using Unity.VisualScripting;

public class MouseRadialMenu : MonoBehaviour
{
	public GameObject prefab;
	public Transform player;
	public EcholocationAbility echolocationAbility;
	private void OnEnable()
	{
		echolocationAbility.Pulsed += Launch;
	}
	private void OnDisable()
	{
		echolocationAbility.Pulsed -= Launch;
	}
	private void Launch(float val)
	{
		transform.GetChild(0).gameObject.SetActive(true);
	}
	public void LaunchEchoTowardsAnimal(int speciesInt)
	{
		transform.GetChild(0).gameObject.SetActive(false);
		Species species = (Species)speciesInt;
		switch (species)
		{
			case Species.Velociraptor:
				FindSpecies<VelociraptorBrain>();
				break;
			case Species.Pterosaur:
				FindSpecies<PterosaurBrain>();
				break;
			case Species.Vulturesaur:
				FindSpecies<VulturesaurBrain>();
				break;
			case Species.Camelsaur:
				FindSpecies<CamelsaurBrain>();
				break;
			case Species.Crocodile:
				FindSpecies<CrocodileBrain>();
				break;
			default: break;
		}
	}

	private void FindSpecies<T>() where T : MonoBehaviour
	{
		var animals = FindObjectsByType<T>(FindObjectsSortMode.None);
		float minDist = float.MaxValue;
		Transform animalClosest =null;
		foreach(MonoBehaviour animal in animals)
		{
			float dist = Vector3.Distance(player.position, animal.transform.position);
			if (dist < minDist)
			{
				minDist = dist;
				animalClosest = animal.transform;
			}
		}
		StartCoroutine(EchoCoruoutine(animalClosest));
	}

	private IEnumerator EchoCoruoutine(Transform animal)
	{
		yield return null;

		for (int i = 0; i < 3; i++)
		{
			yield return new WaitForSeconds(.5f);
			var circleP = Instantiate(prefab, player.position, player.rotation);
			circleP.transform.DOScale(100, 10);
		}
		yield return 2;
		
		for (int i = 0; i < 15; i++)
		{
			if (!animal.gameObject.activeInHierarchy) yield break;
			var circleP = Instantiate(prefab, animal.position, player.rotation);
			circleP.transform.DOScale(100, 15);
			yield return new WaitForSeconds(2);
		}
	}
}