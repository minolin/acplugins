using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinoRatingPlugin
{
    public class CollisionBag
    {
        const int MaximumTreeDurationSeconds = 20;
        const int TreeRefreshSeconds = 4;

        public List<byte> CarsInCollisionTree { get; private set; }
        public DateTime Started { get; private set; }
        public DateTime LastCollision { get; private set; }

        public bool IsActive
        {
            get
            {
                return DateTime.Now.Subtract(LastCollision).TotalSeconds < TreeRefreshSeconds
                    && DateTime.Now.Subtract(Started).TotalSeconds < MaximumTreeDurationSeconds;
            }
        }

        public int Count { get { return CarsInCollisionTree.Count; } }
        public int First { get { return CarsInCollisionTree[0]; } }
        public int Second { get { return CarsInCollisionTree[1]; } }

        internal static CollisionBag StartNew(byte carId, byte otherCarId, Action<CollisionBag> evaluateContactTree)
        {
            var bag = StartNew(carId, otherCarId);
            new Thread(() =>
            {
                try
                {

                    while (true)
                    {
                        var secondsToMaximum = DateTime.Now.Subtract(bag.Started).TotalSeconds;
                        var secondsToRefresh = DateTime.Now.Subtract(bag.LastCollision).TotalSeconds;

                        if (secondsToMaximum > MaximumTreeDurationSeconds || secondsToRefresh > TreeRefreshSeconds)
                            break;

                        Thread.Sleep((int)((TreeRefreshSeconds - secondsToRefresh) * 1000) + 100);
                    }
                    evaluateContactTree(bag);
                }
                catch (Exception)
                {
                    Console.WriteLine("Exception in Collision bag for " + carId + "/" + otherCarId);
                }
            }).Start();

            return bag;
        }

        private static CollisionBag StartNew(byte car1, byte car2)
        {
            var now = DateTime.Now;
            var bag = new CollisionBag()
            {
                CarsInCollisionTree = new List<byte>(),
                Started = now,
                LastCollision = now,
            };

            bag.CarsInCollisionTree.Add(car1);
            bag.CarsInCollisionTree.Add(car2);

            return bag;
        }

        public bool TryAdd(byte car1, byte car2)
        {
            // We'll see if the requirement are met so the collision event can be part of this tree.
            // In reality this means that both aren't to be accused, only the initial contact partners.
            // If not, TryAdd will return false

            // First: Is this bag still valid?
            if (!IsActive)
                return false;

            // Then: Is at least one in the bag?
            if (!CarsInCollisionTree.Contains(car1) && !CarsInCollisionTree.Contains(car2))
                return false;

            // So we should find out which one is and add the other one, if not yet happened (shouldn't, according to this code)
            if (CarsInCollisionTree.Contains(car1) && !CarsInCollisionTree.Contains(car2))
                CarsInCollisionTree.Add(car2);
            if (CarsInCollisionTree.Contains(car2) && !CarsInCollisionTree.Contains(car1))
                CarsInCollisionTree.Add(car1);

            // And: The wreckfest goes on, so we'll notice the this occurance (see TreeRefreshSeconds)
            LastCollision = DateTime.Now;

            return true;
        }
    }
}

