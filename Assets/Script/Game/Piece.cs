using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {
    public bool isWhite, isKing;
    public Vector2Int cell, oldcell;

    private Animator anim;
	// Use this for initialization
	void Awake () {
        anim = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void King()
    {
        isKing = true;
        anim.SetTrigger("King");
    }
}
