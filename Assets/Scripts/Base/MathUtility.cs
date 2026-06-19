using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtility {

	// 角度を計算(ラジアン表記)
	public static float calcRadian2D(Vector2 center, Vector2 target)
	{
		// 二点間の距離
		float dx = target.x - center.x;
		float dy = target.y - center.y;

		// 角度を計算
		float rad = Mathf.Atan2(dy,dx);

		// マイナスの場合
		if (rad < 0.0f) {
			rad = Mathf.PI*2.0f + rad;
		}

		return rad;
	}

	// 角度を計算(Degree表記)
	public static float calcDegree2D(Vector2 center, Vector2 target)
	{
		// ラジアンを変換
		float rad = calcRadian2D (center, target);
		float deg = rad * Mathf.Rad2Deg;

		return deg;
	}
}
