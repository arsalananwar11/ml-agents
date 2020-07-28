using UnityEngine;
using NUnit.Framework;
using Unity.MLAgents.Extensions.Sensors;

namespace Unity.MLAgents.Extensions.Tests.Sensors
{
    public class PoseExtractorTests
    {
        class UselessPoseExtractor : PoseExtractor
        {
            protected internal override Pose GetPoseAt(int index)
            {
                return Pose.identity;
            }

            protected internal override Vector3 GetLinearVelocityAt(int index)
            {
                return Vector3.zero;
            }

            public void Init(int[] parentIndices)
            {
                Setup(parentIndices);
            }
        }

        [Test]
        public void TestEmptyExtractor()
        {
            var poseExtractor = new UselessPoseExtractor();

            // These should be no-ops
            poseExtractor.UpdateLocalSpacePoses();
            poseExtractor.UpdateModelSpacePoses();

            Assert.AreEqual(0, poseExtractor.NumPoses);
        }

        [Test]
        public void TestSimpleExtractor()
        {
            var poseExtractor = new UselessPoseExtractor();
            var parentIndices = new[] { -1, 0 };
            poseExtractor.Init(parentIndices);
            Assert.AreEqual(2, poseExtractor.NumPoses);
        }


        /// <summary>
        /// A simple "chain" hierarchy, where each object is parented to the one before it.
        ///   0 <- 1 <- 2 <- ...
        /// </summary>
        class ChainPoseExtractor : PoseExtractor
        {
            public Vector3 offset;
            public ChainPoseExtractor(int size)
            {
                var parents = new int[size];
                for (var i = 0; i < size; i++)
                {
                    parents[i] = i - 1;
                }
                Setup(parents);
            }

            protected internal override Pose GetPoseAt(int index)
            {
                var rotation = Quaternion.identity;
                var translation = offset + new Vector3(index, index, index);
                return new Pose
                {
                    rotation = rotation,
                    position = translation
                };
            }

            protected  internal override Vector3 GetLinearVelocityAt(int index)
            {
                return Vector3.zero;
            }

        }

        [Test]
        public void TestChain()
        {
            var size = 4;
            var chain = new ChainPoseExtractor(size);
            chain.offset = new Vector3(.5f, .75f, .333f);

            chain.UpdateModelSpacePoses();
            chain.UpdateLocalSpacePoses();

            // Root transforms are currently always the identity.
            Assert.IsTrue(chain.ModelSpacePoses[0] == Pose.identity);
            Assert.IsTrue(chain.LocalSpacePoses[0] == Pose.identity);

            // Check the non-root transforms
            for (var i = 1; i < size; i++)
            {
                var modelSpace = chain.ModelSpacePoses[i];
                var expectedModelTranslation = new Vector3(i, i, i);
                Assert.IsTrue(expectedModelTranslation == modelSpace.position);

                var localSpace = chain.LocalSpacePoses[i];
                var expectedLocalTranslation = new Vector3(1, 1, 1);
                Assert.IsTrue(expectedLocalTranslation == localSpace.position);
            }
        }

    }

    public class PoseExtensionTests
    {
        [Test]
        public void TestInverse()
        {
            Pose t = new Pose
            {
                rotation = Quaternion.AngleAxis(23.0f, new Vector3(1, 1, 1).normalized),
                position = new Vector3(-1.0f, 2.0f, 3.0f)
            };

            var inverseT = t.Inverse();
            var product = inverseT.Multiply(t);
            Assert.IsTrue(Vector3.zero == product.position);
            Assert.IsTrue(Quaternion.identity == product.rotation);

            Assert.IsTrue(Pose.identity == product);
        }

    }
}