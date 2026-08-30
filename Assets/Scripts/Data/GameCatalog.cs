using System.Collections.Generic;
using UnityEngine;

namespace SuperRacing.Data
{
    [CreateAssetMenu(fileName = "GameCatalog", menuName = "Super Racing/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        [SerializeField] private List<CarDefinition> cars = new();
        [SerializeField] private List<TrackDefinition> tracks = new();

        public IReadOnlyList<CarDefinition> Cars => cars;
        public IReadOnlyList<TrackDefinition> Tracks => tracks;
    }
}
