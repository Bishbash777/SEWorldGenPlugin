﻿using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using SEWorldGenPlugin.Generator;
using SEWorldGenPlugin.GUI.Controls;
using SEWorldGenPlugin.ObjectBuilders;
using SEWorldGenPlugin.Session;
using SEWorldGenPlugin.Utilities;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace SEWorldGenPlugin.GUI
{
    public partial class MyPluginAdminMenu
    {
        private long m_spawnType = 0L;

        private long m_asteroidType = 0;

        private MyGuiControlCombobox m_spawnTypeCombo;

        private MyGuiControlCombobox m_asteroidTypeCombo;

        private MyGuiControlListbox m_planetDefList;

        private MyGuiControlTextbox m_nameBox;

        private MyGuiControlClickableSlider m_planetSizeSlider;

        private MyGuiControlButton m_spawnPlanetButton;

        MyGuiControlButton m_spawnPlanetCoordsButton;

        private void BuildSpawnMenu()
        {
            MyPluginLog.Debug("Create Spawn Menu");

            var topCombo = GetCombo();
            Vector2 start = topCombo.Position + new Vector2(0, MARGIN_VERT * 2 + GetCombo().Size.Y);
            Vector2 end = start + new Vector2(topCombo.Size.X, 0.8f - MARGIN_VERT);

            MyGuiControlParentTableLayout table = new MyGuiControlParentTableLayout(1, false, Vector2.Zero);
            table.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            m_spawnTypeCombo = new MyGuiControlCombobox();
            m_spawnTypeCombo.AddItem(0L, "Planet");
            m_spawnTypeCombo.AddItem(1L, "Asteroid object");
            m_spawnTypeCombo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_spawnTypeCombo.SelectItemByKey(m_spawnType);
            m_spawnTypeCombo.ItemSelected += OnSpawnTypeChange;
            m_spawnTypeCombo.Size = new Vector2(m_usableWidth * 0.9f, m_spawnTypeCombo.Size.Y);

            table.AddTableRow(m_spawnTypeCombo);

            table.AddTableSeparator();

            switch (m_spawnType)
            {
                case 0L:
                    CreatePlanetSpawnMenu(table);
                    break;
                case 1L:
                    CreateAsteroidSpawnMenu(table);
                    break;
            }

            table.AddTableSeparator();

            table.ApplyRows();

            MyGuiControlScrollablePanel scrollPane = new MyGuiControlScrollablePanel(table);
            scrollPane.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            scrollPane.ScrollbarVEnabled = true;
            scrollPane.Size = end - start;
            scrollPane.Size = new Vector2(0.315f, scrollPane.Size.Y);
            scrollPane.Position = new Vector2(0, start.Y);

            Controls.Add(scrollPane);

            MyGuiControlSeparatorList sep = new MyGuiControlSeparatorList();
            sep.AddHorizontal(new Vector2(scrollPane.Position.X - scrollPane.Size.X / 2, scrollPane.Position.Y + scrollPane.Size.Y), m_usableWidth);

            Controls.Add(sep);

            MyPluginLog.Debug("Added spawn menu");
        }

        private void CreatePlanetSpawnMenu(MyGuiControlParentTableLayout table)
        {
            var settings = MySettingsSession.Static.Settings.GeneratorSettings;

            MyGuiControlLabel defLabel = new MyGuiControlLabel(null, null, "Planet");

            table.AddTableRow(defLabel);

            m_planetDefList = new MyGuiControlListbox();
            m_planetDefList.VisibleRowsCount = 8;
            m_planetDefList.MultiSelect = false;

            table.AddTableRow(m_planetDefList);

            table.AddTableSeparator();

            MyGuiControlLabel sizeLabel = new MyGuiControlLabel(null, null, "Planet size");

            table.AddTableRow(sizeLabel);

            m_planetSizeSlider = new MyGuiControlClickableSlider(null, 1f, settings.PlanetSettings.PlanetSizeCap, m_usableWidth - 0.1f, intValue: true, showLabel: true, labelSuffix: " m");

            table.AddTableRow(m_planetSizeSlider);

            table.AddTableSeparator();

            MyGuiControlLabel nameLabel = new MyGuiControlLabel(null, null, "Name");

            table.AddTableRow(nameLabel);

            m_nameBox = new MyGuiControlTextbox();
            m_nameBox.Size = new Vector2(m_usableWidth, m_nameBox.Size.Y);

            table.AddTableRow(m_nameBox);

            table.AddTableSeparator();

            m_spawnPlanetButton = MyPluginGuiHelper.CreateDebugButton(m_usableWidth, "Spawn planet", delegate (MyGuiControlButton button)
            {
                OnSpawnPlanet();
            });
            m_spawnPlanetButton.Enabled = false;

            table.AddTableRow(m_spawnPlanetButton);

            m_spawnPlanetCoordsButton = MyPluginGuiHelper.CreateDebugButton(m_usableWidth, "Spawn planet at coordinates", delegate(MyGuiControlButton button)
            {
                OnSpawnPlanet(true);
            });
            m_spawnPlanetCoordsButton.Enabled = false;

            table.AddTableRow(m_spawnPlanetCoordsButton);

            m_planetDefList.ItemClicked += delegate(MyGuiControlListbox box)
            {
                if(box.SelectedItems[box.SelectedItems.Count-1] != null)
                {
                    m_spawnPlanetButton.Enabled = true;
                    m_spawnPlanetCoordsButton.Enabled = true;
                }
                else
                {
                    m_spawnPlanetButton.Enabled = false;
                    m_spawnPlanetCoordsButton.Enabled = false;
                }
            };

            LoadPlanetDefs(m_planetDefList);
        }

        private void CreateAsteroidSpawnMenu(MyGuiControlParentTableLayout table)
        {
            m_asteroidTypeCombo = new MyGuiControlCombobox();

            foreach(var provider in m_asteroidProviders)
            {
                m_asteroidTypeCombo.AddItem(m_asteroidProviders.IndexOf(provider), provider.GetTypeName());
            }

            m_asteroidTypeCombo.SelectItemByKey(m_asteroidType);
            m_asteroidTypeCombo.ItemSelected += delegate
            {
                m_asteroidType = m_asteroidTypeCombo.GetSelectedKey();
                RecreateControls(false);
            };
            m_asteroidTypeCombo.Size = new Vector2(m_usableWidth * 0.9f, m_asteroidTypeCombo.Size.Y);

            table.AddTableRow(m_asteroidTypeCombo);

            m_asteroidProviders[(int)m_asteroidType].GetAdminMenuCreator().CreateSpawnMenu(m_usableWidth, table, this);
        }

        private void OnSpawnPlanet(bool coordSpawn = false)
        {
            StringBuilder name = new StringBuilder();
            m_nameBox.GetText(name);
            if(name.ToString().Trim().Length <= 3)
            {
                MyPluginGuiHelper.DisplayError("The name must be at least 4 letters long", "Error");
                return;
            }

            if (coordSpawn)
            {
                MyGuiScreenDialogCoordinate coordinateInput = new MyGuiScreenDialogCoordinate("Planet coordinate");

                coordinateInput.OnConfirmed += delegate (Vector3D coord)
                {
                    MySystemPlanet p = new MySystemPlanet()
                    {
                        CenterPosition = coord,
                        SubtypeId = ((MyPlanetGeneratorDefinition)m_planetDefList.GetLastSelected().UserData).Id.SubtypeId.ToString(),
                        Generated = false,
                        DisplayName = name.ToString().Trim(),
                        Diameter = m_planetSizeSlider.Value
                    };

                    SpawnPlanet(p, coord);
                };

                MyGuiSandbox.AddScreen(coordinateInput);
                return;
            }

            float size = m_planetSizeSlider.Value;
            MySystemPlanet planet = new MySystemPlanet()
            {
                CenterPosition = Vector3D.Zero,
                SubtypeId = ((MyPlanetGeneratorDefinition)m_planetDefList.GetLastSelected().UserData).Id.SubtypeId.ToString(),
                Generated = false,
                DisplayName = name.ToString().Trim(),
                Diameter = size
            };

            MyPluginItemsClipboard.Static.Activate(planet, SpawnPlanet, size);

            CloseScreenNow();
        }

        private void OnSpawnTypeChange()
        {
            m_spawnType = m_spawnTypeCombo.GetSelectedKey();
            RecreateControls(false);
        }

        /// <summary>
        /// Spawns a planet in the system
        /// </summary>
        /// <param name="planet">Planet to spawn</param>
        /// <param name="position">Position to spawn at</param>
        private void SpawnPlanet(MySystemObject planet, Vector3D position)
        {
            if (planet.Type == MySystemObjectType.PLANET)
            {
                MySystemPlanet p = planet as MySystemPlanet;
                p.CenterPosition = position;

                MyStarSystemGenerator.Static.AddObjectToSystem(p, callback: delegate (bool success)
                {
                    if (!success)
                    {
                        MyPluginGuiHelper.DisplayError("Planet could not be spawned, because an object with the name already exists", "Error");
                    }
                });
            }
        }

        /// <summary>
        /// Puts all loaded planet definitions into the given listbox.
        /// </summary>
        /// <param name="listBox">Listbox</param>
        private void LoadPlanetDefs(MyGuiControlListbox listBox)
        {
            listBox.Clear();
            var definitions = MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions();
            foreach(var planet in definitions)
            {
                MyGuiControlListbox.Item i = new MyGuiControlListbox.Item(new StringBuilder(planet.Id.SubtypeId.ToString()), userData: planet);
                listBox.Items.Add(i);
            }
        }
    }
}