﻿using System;
using System.Collections.Generic;
using System.Linq;
using Codartis.NsDepCop.Core.Implementation.Analysis;
using Codartis.NsDepCop.Core.Interface.Analysis;
using Codartis.NsDepCop.Core.Interface.Config;
using FluentAssertions;
using Moq;
using Xunit;

namespace Codartis.NsDepCop.Core.Test.Implementation.Analysis
{
    public class DependencyAnalyzerTests
    {
        private static readonly SourceSegment DummySourceSegment = new SourceSegment(1, 1, 1, 1, null, null);

        private readonly Mock<IConfigProvider> _configProviderMock = new Mock<IConfigProvider>();
        private readonly Mock<ITypeDependencyEnumerator> _typeDependencyEnumeratorMock = new Mock<ITypeDependencyEnumerator>();

        [Theory]
        [InlineData(AnalyzerConfigState.NoConfig)]
        [InlineData(AnalyzerConfigState.ConfigError)]
        [InlineData(AnalyzerConfigState.Disabled)]
        public void AnalyzeSyntaxNode_ConfigNotEnabled_Exception(AnalyzerConfigState configState)
        {
            _configProviderMock.Setup(i => i.ConfigState).Returns(configState);

            var dependencyAnalyzer = CreateDependencyAnalyzer();
            Assert.Throws<InvalidOperationException>(() => dependencyAnalyzer.AnalyzeSyntaxNode(null, null));
        }

        [Fact]
        public void AnalyzeSyntaxNode_NoTypeDependencies_EmptyResult()
        {
            SetUpEnabledConfig();

            var dependencyAnalyzer = CreateDependencyAnalyzer();
            dependencyAnalyzer.AnalyzeSyntaxNode(null, null).Should().BeEmpty();
        }

        [Theory]
        [InlineData(AnalyzerConfigState.NoConfig)]
        [InlineData(AnalyzerConfigState.ConfigError)]
        [InlineData(AnalyzerConfigState.Disabled)]
        public void AnalyzeProject_ConfigNotEnabled_Exception(AnalyzerConfigState configState)
        {
            _configProviderMock.Setup(i => i.ConfigState).Returns(configState);

            var dependencyAnalyzer = CreateDependencyAnalyzer();
            Assert.Throws<InvalidOperationException>(() => dependencyAnalyzer.AnalyzeProject(null, null));
        }

        [Fact]
        public void AnalyzeProject_NoTypeDependencies_EmptyResult()
        {
            SetUpEnabledConfig();

            var dependencyAnalyzer = CreateDependencyAnalyzer();
            dependencyAnalyzer.AnalyzeProject(null, null).Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeProject_TypeDependenciesReturned()
        {
            SetUpEnabledConfig();

            _typeDependencyEnumeratorMock
                .Setup(i => i.GetTypeDependencies(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .Returns(Enumerable.Repeat(new TypeDependency("N1", "T1", "N2", "T2", DummySourceSegment), 2));

            var dependencyAnalyzer = CreateDependencyAnalyzer();
            dependencyAnalyzer.AnalyzeProject(null, null).Should().HaveCount(2);
        }

        [Fact]
        public void AnalyzeProject_MaxIssueCountHonored()
        {
            SetUpEnabledConfig(maxIssueCount: 2);

            _typeDependencyEnumeratorMock
                .Setup(i => i.GetTypeDependencies(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .Returns(Enumerable.Repeat(new TypeDependency("N1", "T1", "N2", "T2", DummySourceSegment), 3));

            var dependencyAnalyzer = CreateDependencyAnalyzer();
            dependencyAnalyzer.AnalyzeProject(null, null).Should().HaveCount(2);
        }

        [Fact]
        public void RefreshConfig_Works()
        {
            SetUpEnabledConfig(maxIssueCount: 2);

            _typeDependencyEnumeratorMock
                .Setup(i => i.GetTypeDependencies(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .Returns(Enumerable.Repeat(new TypeDependency("N1", "T1", "N2", "T2", DummySourceSegment), 10));

            var dependencyAnalyzer = CreateDependencyAnalyzer();
            dependencyAnalyzer.AnalyzeProject(null, null).Should().HaveCount(2);

            SetUpEnabledConfig(maxIssueCount: 4);
            dependencyAnalyzer.RefreshConfig();
            dependencyAnalyzer.AnalyzeProject(null, null).Should().HaveCount(4);
        }

        private void SetUpEnabledConfig(int maxIssueCount = 100)
        {
            var analyzerConfigMock = new Mock<IAnalyzerConfig>();
            analyzerConfigMock.Setup(i => i.AllowRules).Returns(new Dictionary<NamespaceDependencyRule, TypeNameSet>());
            analyzerConfigMock.Setup(i => i.DisallowRules).Returns(new HashSet<NamespaceDependencyRule>());
            analyzerConfigMock.Setup(i => i.VisibleTypesByNamespace).Returns(new Dictionary<Namespace, TypeNameSet>());
            analyzerConfigMock.Setup(i => i.MaxIssueCount).Returns(maxIssueCount);

            _configProviderMock.Setup(i => i.ConfigState).Returns(AnalyzerConfigState.Enabled);
            _configProviderMock.Setup(i => i.ConfigException).Returns<Exception>(null);
            _configProviderMock.Setup(i => i.Config).Returns(analyzerConfigMock.Object);
        }

        private DependencyAnalyzer CreateDependencyAnalyzer()
            => new DependencyAnalyzer(_configProviderMock.Object, _typeDependencyEnumeratorMock.Object);
    }
}
