using System.Text.Json.Nodes;
using Bunit;
using FluentAssertions;
using MudExtensions;
using MudExtensions.Docs.Examples;
using MudExtensions.UnitTests.Mocks;
using NUnit.Framework;

namespace CodeBeam.MudBlazor.Extensions.UnitTests.Components
{
    [TestFixture]
    public class JsonTreeViewTests : BunitTest
    {
        [Test]
        public void MudJsonTreeView_ShouldGenerateFullJsonPath()
        {
            // Arrange
            var json = @"
            {
              'PatientRecordType': 'v4.0.8',
              'DoctorId': '8F704CD5-3CCE-4CC0-957C-2BC98DC06E42',
              'PatientId': '0000-0002-6',
              'Birthdate': '1980-02-15T00:00:00Z',
              'UpdateDate': '2023-02-14T07:12:27.2502767-05:00',
              'Races': [
                'Vietnamese',
                'Caucasian'
              ],
              'Status': 'AV',
              'Gender': 'M',
              'Active': true,
              'Type': 'Normal'
            }".Replace("'", "\"");

            var component = RenderComponent<MudJsonTreeView>(parameters => parameters
                .Add(p => p.Json, json)
                .Add(p => p.OnNodeSelected, EventCallback.Factory.Create<(JsonNode, string)>(this, HandleNodeSelected))
            );

            // Act
            var node = component.FindAll("div.mud-treeview-item")[1];
            node.Click();

            // Assert
            _selectedNodePath.Should().Be("DoctorId");
        }

        [Test]
        public void MudJsonTreeViewNode_ShouldGenerateFullJsonPath()
        {
            // Arrange
            var json = @"
            {
              'PatientRecordType': 'v4.0.8',
              'DoctorId': '8F704CD5-3CCE-4CC0-957C-2BC98DC06E42',
              'PatientId': '0000-0002-6',
              'Birthdate': '1980-02-15T00:00:00Z',
              'UpdateDate': '2023-02-14T07:12:27.2502767-05:00',
              'Races': [
                'Vietnamese',
                'Caucasian'
              ],
              'Status': 'AV',
              'Gender': 'M',
              'Active': true,
              'Type': 'Normal'
            }".Replace("'", "\"");

            var rootNode = JsonNode.Parse(json);
            var component = RenderComponent<MudJsonTreeViewNode>(parameters => parameters
                .Add(p => p.Node, rootNode)
                .Add(p => p.Sorted, false)
                .Add(p => p.OnNodeSelected, EventCallback.Factory.Create<(JsonNode, string)>(this, HandleNodeSelected))
            );

            // Act
            var node = component.FindAll("div.mud-treeview-item")[1];
            node.Click();

            // Assert
            _selectedNodePath.Should().Be("DoctorId");
        }

        private string? _selectedNodePath;

        private void HandleNodeSelected((JsonNode node, string path) nodeInfo)
        {
            _selectedNodePath = nodeInfo.path;
        }
    }
}
