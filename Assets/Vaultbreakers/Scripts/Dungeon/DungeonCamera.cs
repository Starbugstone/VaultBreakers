using UnityEngine;
namespace Vaultbreakers.Dungeon
{
    [DefaultExecutionOrder(15)]
    public sealed class DungeonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        private Vector3 focus;
        private Vaultbreakers.Zones.ZoneController zone;
        public Vector3 RestPosition { get; private set; }
        public void Configure(Transform player)=>target=player;
        private void Start(){zone=Object.FindAnyObjectByType<Vaultbreakers.Zones.ZoneController>();focus=zone.RoomOrigin;Apply();}
        private void LateUpdate()
        {
            if(target==null)return;
            var destination=zone.State==Vaultbreakers.Zones.ZoneState.BetweenWaves ? new Vector3(0,0,Mathf.Max(zone.RoomOrigin.z,target.position.z)) : zone.RoomOrigin;
            focus=Vector3.Lerp(focus,destination,1-Mathf.Exp(-6*Time.unscaledDeltaTime));Apply();
        }
        private void Apply()
        {
            RestPosition=focus+new Vector3(10,14.7f,-10);transform.position=RestPosition;
            transform.rotation=Quaternion.LookRotation(new Vector3(-10,-14,10));
        }
    }
}
