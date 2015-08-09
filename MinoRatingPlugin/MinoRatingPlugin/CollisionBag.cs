using acPlugins4net.helpers;
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

        public bool IsActive { get; set; }
        private readonly object lockObject = new object();

        public int Count { get { return CarsInCollisionTree.Count; } }
        public int First { get { return CarsInCollisionTree[0]; } }
        public int Second { get { return CarsInCollisionTree[1]; } }

        internal static CollisionBag StartNew(byte carId, byte otherCarId, Action<CollisionBag> evaluateContactTree, ILog log)
        {
            var bag = StartNew(carId, otherCarId);
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    while (true)
                    {
                        double secondsToMaximum, secondsToRefresh;

                        lock (bag.lockObject)
                        {
                            secondsToMaximum = DateTime.Now.Subtract(bag.Started).TotalSeconds;
                            secondsToRefresh = DateTime.Now.Subtract(bag.LastCollision).TotalSeconds;

                            if (secondsToMaximum > MaximumTreeDurationSeconds || secondsToRefresh > TreeRefreshSeconds)
                            {
                                bag.IsActive = false;
                                break;
                            }
                        }

                        var duration = (TreeRefreshSeconds - secondsToRefresh) * 1000 + 100;
                        Thread.Sleep((int)(duration));
                    }
                    log.Log("" + DateTime.Now.TimeOfDay + ": Collision bag finished (" + carId + " vs. " + otherCarId + ")");
                    evaluateContactTree(bag);
                }
                catch (Exception ex)
                {
                    log.Log("Exception in Collision bag for " + carId + "/" + otherCarId);
                    log.Log(ex);
                }
            });

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
                IsActive = true,
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
            lock (lockObject)
            {
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
}

