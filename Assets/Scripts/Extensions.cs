using UnityEngine;
using System.Collections;

public static class Extensions {

	public static void SetLocalScaleX(this Transform t, float newX) {
		t.localScale = new Vector3(newX, t.localScale.y, t.localScale.z);
	}

	public static void SetLocalScaleY(this Transform t, float newY) {
		t.localScale = new Vector3(t.localScale.x, newY, t.localScale.z);
	}

	public static void SetLocalScaleZ(this Transform t, float newZ) {
		t.localScale = new Vector3(t.localScale.x, t.localScale.y, newZ);
	}

}