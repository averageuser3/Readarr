﻿using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class QualityTest : TestBase
    {
        [Test]
        [Ignore("No supported asserts are available")]
        public void Icomparer_greater_test()
        {
            var first = new Quality(QualityTypes.DVD, true);
            var second = new Quality(QualityTypes.Bluray1080p, true);

            //Assert.GreaterThan(second, first);
        }

        [Test]
        [Ignore("No supported asserts are available")]
        public void Icomparer_greater_proper()
        {
            var first = new Quality(QualityTypes.Bluray1080p, false);
            var second = new Quality(QualityTypes.Bluray1080p, true);

            //Assert.GreaterThan(second, first);
        }

        [Test]
        [Ignore("No supported asserts are available")]
        public void Icomparer_lesser()
        {
            var first = new Quality(QualityTypes.DVD, true);
            var second = new Quality(QualityTypes.Bluray1080p, true);

            //Assert.LessThan(first, second);
        }

        [Test]
        [Ignore("No supported asserts are available")]
        public void Icomparer_lesser_proper()
        {
            var first = new Quality(QualityTypes.DVD, false);
            var second = new Quality(QualityTypes.DVD, true);

            //Assert.LessThan(first, second);
        }

        [Test]
        public void equal_operand()
        {
            var first = new Quality(QualityTypes.Bluray1080p, true);
            var second = new Quality(QualityTypes.Bluray1080p, true);

            Assert.IsTrue(first == second);
            Assert.IsTrue(first >= second);
            Assert.IsTrue(first <= second);
        }

        [Test]
        public void equal_operand_false()
        {
            var first = new Quality(QualityTypes.Bluray1080p, true);
            var second = new Quality(QualityTypes.Unknown, true);

            Assert.IsFalse(first == second);
        }

        [Test]
        public void equal_operand_false_proper()
        {
            var first = new Quality(QualityTypes.Bluray1080p, true);
            var second = new Quality(QualityTypes.Bluray1080p, false);

            Assert.IsFalse(first == second);
        }


        [Test]
        public void not_equal_operand()
        {
            var first = new Quality(QualityTypes.Bluray1080p, true);
            var second = new Quality(QualityTypes.Bluray1080p, true);

            Assert.IsFalse(first != second);
        }

        [Test]
        public void not_equal_operand_false()
        {
            var first = new Quality(QualityTypes.Bluray1080p, true);
            var second = new Quality(QualityTypes.Unknown, true);

            Assert.IsTrue(first != second);
        }

        [Test]
        public void not_equal_operand_false_proper()
        {
            var first = new Quality(QualityTypes.Bluray1080p, true);
            var second = new Quality(QualityTypes.Bluray1080p, false);

            Assert.IsTrue(first != second);
        }

        [Test]
        public void greater_operand()
        {
            var first = new Quality(QualityTypes.DVD, true);
            var second = new Quality(QualityTypes.Bluray1080p, true);

            Assert.IsTrue(first < second);
            Assert.IsTrue(first <= second);
        }

        [Test]
        public void lesser_operand()
        {
            var first = new Quality(QualityTypes.DVD, true);
            var second = new Quality(QualityTypes.Bluray1080p, true);

            Assert.IsTrue(second > first);
            Assert.IsTrue(second >= first);
        }

    }
}
