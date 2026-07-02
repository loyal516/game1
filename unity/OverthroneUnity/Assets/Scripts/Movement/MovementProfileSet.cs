using System.Collections.Generic;
using UnityEngine;

namespace Overthrone
{
    [CreateAssetMenu(menuName = "Overthrone/Movement Profile Set")]
    public sealed class MovementProfileSet : ScriptableObject
    {
        [SerializeField] private List<MovementProfile> profiles = new List<MovementProfile>();

        public IReadOnlyList<MovementProfile> Profiles => profiles;

        public MovementProfile Get(MovementState state)
        {
            for (var i = 0; i < profiles.Count; i += 1)
            {
                if (profiles[i].state == state)
                {
                    return profiles[i];
                }
            }

            return profiles.Count > 0 ? profiles[0] : new MovementProfile();
        }

        public void SetProfiles(IEnumerable<MovementProfile> nextProfiles)
        {
            profiles = new List<MovementProfile>(nextProfiles);
        }
    }
}
