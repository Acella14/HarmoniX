using UnityEngine;

public interface IShockwaveLaunchable {
    void LaunchFromShockwave(Vector3 origin, float force, float radius, int damage);
}
