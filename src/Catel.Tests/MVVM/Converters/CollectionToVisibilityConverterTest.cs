﻿namespace Catel.Tests.MVVM.Converters
{
    using System.Collections.Generic;
    using System.Globalization;
    using Catel.MVVM.Converters;

    using NUnit.Framework;
    using System.Windows;

    [TestFixture]
    public class CollectionToVisibilityConverterTest
    {
        [TestCase]
        public void Convert_Null()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(null, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_EmptyList()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(new List<int>(), typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_FilledList()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Visible, converter.Convert(new List<int>(new[] { 1, 2, 3 }), typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_EmptyString()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(string.Empty, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_String()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Visible, converter.Convert("filledstring", typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_Long_0()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(0L, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_Long_1()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Visible, converter.Convert(1L, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_Int_0()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(0, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_Int_1()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Visible, converter.Convert(1, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_Short_0()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, converter.Convert((short)0, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void Convert_Short_1()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(Visibility.Visible, converter.Convert((short)1, typeof(Visibility), null, (CultureInfo)null));
        }

        [TestCase]
        public void ConvertBack()
        {
            var converter = new CollectionToCollapsingVisibilityConverter();
            Assert.AreEqual(ConverterHelper.UnsetValue, converter.ConvertBack(null, typeof(object), null, (CultureInfo)null));
        }
    }
}
