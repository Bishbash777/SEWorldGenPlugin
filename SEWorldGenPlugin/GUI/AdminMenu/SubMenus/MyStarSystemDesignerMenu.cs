﻿using Sandbox.Graphics.GUI;
using SEWorldGenPlugin.Draw;
using SEWorldGenPlugin.Generator;
using SEWorldGenPlugin.Generator.AsteroidObjects;
using SEWorldGenPlugin.GUI.AdminMenu.SubMenus.StarSystemDesigner;
using SEWorldGenPlugin.GUI.Controls;
using SEWorldGenPlugin.ObjectBuilders;
using SEWorldGenPlugin.Session;
using SEWorldGenPlugin.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace SEWorldGenPlugin.GUI.AdminMenu.SubMenus
{
    /// <summary>
    /// The star system designer admin menu, used to create and edit star systems generated by the plugin.
    /// </summary>
    public class MyStarSystemDesignerMenu : MyPluginAdminMenuSubMenu
    {
        /// <summary>
        /// A list box containing all system objects
        /// </summary>
        private MyGuiControlListbox m_systemObjectsBox;

        /// <summary>
        /// Button used to refresh the gui elements to contain the current system.
        /// </summary>
        private MyGuiControlButton m_refreshSystemButton;

        /// <summary>
        /// Button used to add a new Object to the system.
        /// </summary>
        private MyGuiControlButton m_addObjectButton;

        /// <summary>
        /// Button to apply the changes done to a system object.
        /// </summary>
        private MyGuiControlButton m_applyChangesButton;

        /// <summary>
        /// Button used to zoom into the current selected object.
        /// </summary>
        private MyGuiControlButton m_zoomInButton;

        /// <summary>
        /// Button used to zoom out of the current selected object.
        /// </summary>
        private MyGuiControlButton m_zoomOutButton;

        /// <summary>
        /// Table that holds the controls to set object specific settings.
        /// </summary>
        private MyGuiControlParentTableLayout m_subMenuControlTable;

        /// <summary>
        /// Control to set the name of the edited object
        /// </summary>
        private MyGuiControlTextbox m_objNameBox;

        /// <summary>
        /// Current instance of the admin menu
        /// </summary>
        private MyAdminMenuExtension m_adminMenuInst;

        /// <summary>
        /// A dictionary that stores all currently changed system objects, that have not yet been applied
        /// </summary>
        private Dictionary<Guid, MySystemObject> m_pendingSystemObjects;

        /// <summary>
        /// A dictionary that stores all currently changed system asteroid objects data, that have not yet been applied
        /// </summary>
        private Dictionary<Guid, IMyAsteroidData> m_pendingAsteroidData;

        /// <summary>
        /// The id of the currently selected system object.
        /// </summary>
        private Guid m_selectedObjectId;

        /// <summary>
        /// The usable gui width
        /// </summary>
        private float m_usableWidth;

        /// <summary>
        /// The menu currently used to edit the selected object
        /// </summary>
        private MyStarSystemDesignerObjectMenu m_currentObjectMenu;

        /// <summary>
        /// The renderer used to render the star system
        /// </summary>
        private MyStarSystemDesignerRenderer m_renderer;

        /// <summary>
        /// A dictionary mapping the ids of system objects to the items in the listbox
        /// </summary>
        private Dictionary<Guid, MyGuiControlListbox.Item> m_itemList;

        public MyStarSystemDesignerMenu()
        {
            m_pendingSystemObjects = new Dictionary<Guid, MySystemObject>(); //Needs to be cleaned on session close
            m_pendingAsteroidData = new Dictionary<Guid, IMyAsteroidData>();
            m_itemList = new Dictionary<Guid, MyGuiControlListbox.Item>();
            m_selectedObjectId = Guid.Empty;
        }

        public override void Close()
        {
            MyPluginLog.Debug("Close");
            m_selectedObjectId = Guid.Empty;
            m_adminMenuInst = null;
            m_systemObjectsBox = null;
            //m_renderer = null;
        }

        public override string GetTitle()
        {
            return "Star System Designer";
        }

        public override bool IsVisible()
        {
            return MyPluginSession.Static.ServerVersionMatch;//Star system designer is only visible, IFF server and client version match up
        }

        public override void RefreshInternals(MyGuiControlParentTableLayout parent, float maxWidth, MyAdminMenuExtension instance)
        {
            MyPluginLog.Debug("Building Star system designer admin menu");

            m_adminMenuInst = instance;
            m_usableWidth = maxWidth;
            m_renderer = new MyStarSystemDesignerRenderer();

            MyGuiControlLabel systemBoxLabel = new MyGuiControlLabel(null, null, "System Objects");
            parent.AddTableRow(systemBoxLabel);

            m_systemObjectsBox = new MyGuiControlListbox();
            m_systemObjectsBox.VisibleRowsCount = 8;
            m_systemObjectsBox.Size = new Vector2(maxWidth, m_systemObjectsBox.Size.Y);
            RefreshSystemList();
            m_systemObjectsBox.ItemsSelected += OnSystemObjectSelected;

            parent.AddTableRow(m_systemObjectsBox);

            m_refreshSystemButton = MyPluginGuiHelper.CreateDebugButton("Refresh", RefreshSystem, true);
            m_refreshSystemButton.Size = new Vector2(maxWidth, m_refreshSystemButton.Size.Y);

            parent.AddTableRow(m_refreshSystemButton);

            m_addObjectButton = MyPluginGuiHelper.CreateDebugButton("Add new object", AddNewSystemObject, true);
            m_addObjectButton.Size = new Vector2(maxWidth, m_addObjectButton.Size.Y);

            parent.AddTableRow(m_addObjectButton);
            parent.AddTableSeparator();

            var row = new MyGuiControlParentTableLayout(3, false, Vector2.Zero);

            m_zoomInButton = new MyGuiControlButton(null, VRage.Game.MyGuiControlButtonStyleEnum.Increase, onButtonClick: OnZoomLevelChange, toolTip: "Zoom onto the selected object");
            m_zoomOutButton = new MyGuiControlButton(null, VRage.Game.MyGuiControlButtonStyleEnum.Decrease, onButtonClick: OnZoomLevelChange, toolTip: "Zoom out of the selected object");

            m_zoomInButton.Enabled = m_renderer.FocusZoom != ZoomLevel.OBJECT;
            m_zoomOutButton.Enabled = m_renderer.FocusZoom != ZoomLevel.ORBIT;

            row.AddTableRow(m_zoomInButton, m_zoomOutButton, new MyGuiControlLabel(text: "Zoom in / out"));

            parent.AddTableRow(row);
            parent.AddTableSeparator();

            m_subMenuControlTable = new MyGuiControlParentTableLayout(1, false, Vector2.Zero);

            if(m_selectedObjectId != Guid.Empty)
            {
                SetSubMenuControls();
            }

            parent.AddTableRow(m_subMenuControlTable);
            parent.AddTableSeparator();

            m_applyChangesButton = MyPluginGuiHelper.CreateDebugButton("Apply", AddNewSystemObject, false, "Apply settings of this object and spawn it if it isnt spawned yet.");
            m_applyChangesButton.Size = new Vector2(maxWidth, m_applyChangesButton.Size.Y);

            parent.AddTableRow(m_applyChangesButton);

            MyPluginDrawSession.Static.AddRenderObject(15, m_renderer);

            if (MySettingsSession.Static.Settings.Enabled)
            {
                m_systemObjectsBox.SelectSingleItem(m_itemList[MyStarSystemGenerator.Static.StarSystem.CenterObject.Id]);
                OnSystemObjectSelected(m_systemObjectsBox);
            }
        }

        public override void Draw()
        {
            m_renderer.Draw();
        }

        /// <summary>
        /// Creates the sub menu controls, based on the type of selected object and whether it already exists or not.
        /// Enables the add object button, if the selected object exists in the star system, and if the type supports child objects.
        /// Enables the apply button if the object was edited.
        /// </summary>
        private void SetSubMenuControls()
        {
            m_subMenuControlTable.ClearTable();

            var StarSystem = MyStarSystemGenerator.Static.StarSystem;
            bool exists = StarSystem.Contains(m_selectedObjectId);
            MySystemObject obj;
            if (m_pendingSystemObjects.ContainsKey(m_selectedObjectId))
            {
                obj = m_pendingSystemObjects[m_selectedObjectId];
                m_applyChangesButton.Enabled = true;
            }
            else if (exists)
            {
                obj = StarSystem.GetById(m_selectedObjectId);
            }
            else return;

            m_objNameBox = new MyGuiControlTextbox();
            m_objNameBox.Size = new Vector2(m_usableWidth, m_objNameBox.Size.Y);
            m_objNameBox.SetToolTip(new MyToolTips("Sets the name of the system object"));
            m_objNameBox.SetText(new StringBuilder(obj.DisplayName));
            m_objNameBox.TextChanged += delegate
            {
                StringBuilder sb = new StringBuilder();
                m_objNameBox.GetText(sb);

                obj.DisplayName = sb.ToString();
                OnObjectEdited(obj);
            };

            m_addObjectButton.Enabled = exists;

            m_subMenuControlTable.AddTableRow(new MyGuiControlLabel(text: "Name"));
            m_subMenuControlTable.AddTableRow(m_objNameBox);

            if(obj.Type == MySystemObjectType.PLANET || obj.Type == MySystemObjectType.MOON)
            {
                BuildPlanetMenuControls(exists, obj as MySystemPlanet);
            }
            else if(obj.Type == MySystemObjectType.ASTEROIDS)
            {
                MySystemAsteroids asteroid = obj as MySystemAsteroids;
                MyAbstractAsteroidObjectProvider prov;
                if (MyAsteroidObjectsManager.Static.AsteroidObjectProviders.TryGetValue(asteroid.AsteroidTypeName, out prov))
                {
                    IMyAsteroidData data = null;
                    if (m_pendingAsteroidData.ContainsKey(asteroid.Id))
                    {
                        data = m_pendingAsteroidData[asteroid.Id];
                    }

                    var adminMenu = prov.CreateStarSystemDesignerEditMenu(asteroid, data);
                    if(adminMenu != null)
                    {
                        if (m_currentObjectMenu != null)
                            m_currentObjectMenu.OnObjectChanged -= OnObjectEdited;

                        adminMenu.RecreateControls(m_subMenuControlTable, m_usableWidth, exists);
                        adminMenu.OnObjectChanged += OnObjectEdited;
                        m_currentObjectMenu = adminMenu;

                        m_addObjectButton.Enabled = m_currentObjectMenu.CanAddChild && exists;
                    }
                }
            }
            m_adminMenuInst.RequestResize();
        }

        /// <summary>
        /// Creates the controls to edit or spawn a planet.
        /// </summary>
        /// <param name="exists">If the planet already exists in the world.</param>
        /// <param name="planet">The planet object itself</param>
        private void BuildPlanetMenuControls(bool exists, MySystemPlanet planet)
        {
            if(m_currentObjectMenu != null)
                m_currentObjectMenu.OnObjectChanged -= OnObjectEdited;

            m_currentObjectMenu = new MyStarSystemDesignerPlanetMenu(planet);
            m_currentObjectMenu.RecreateControls(m_subMenuControlTable, m_usableWidth, exists);
            m_currentObjectMenu.OnObjectChanged += OnObjectEdited;

            m_addObjectButton.Enabled = m_currentObjectMenu.CanAddChild;
        }

        /// <summary>
        /// Called from sub menus when object gets edited.
        /// </summary>
        /// <param name="obj"></param>
        private void OnObjectEdited(MySystemObject obj)
        {
            StringBuilder name = new StringBuilder();
            m_objNameBox.GetText(name);

            obj.DisplayName = name.ToString();

            int depth = MyStarSystemGenerator.Static.StarSystem.GetDepth(obj.Id);
            if(depth < 0)
            {
                depth = MyStarSystemGenerator.Static.StarSystem.GetDepth(obj.ParentId) + 1;
            }
            if(depth > 0)
            {
                name.Insert(0, "   ", depth);
            }
            name.Append(" *");


            m_systemObjectsBox.SelectedItems[0].Text = name;

            m_applyChangesButton.Enabled = true;

            if (!m_pendingSystemObjects.ContainsKey(obj.Id))
            {
                m_pendingSystemObjects.Add(obj.Id, obj);

                if(obj.Type == MySystemObjectType.ASTEROIDS)
                {
                    var roid = obj as MySystemAsteroids;
                    if(MyAsteroidObjectsManager.Static.AsteroidObjectProviders.TryGetValue(roid.AsteroidTypeName, out MyAbstractAsteroidObjectProvider prov))
                    {
                        m_pendingAsteroidData.Add(obj.Id, prov.GetInstanceData(obj.Id));
                    }
                }
            }
        }

        /// <summary>
        /// When a new System object is selected, update GUI
        /// </summary>
        /// <param name="box"></param>
        private void OnSystemObjectSelected(MyGuiControlListbox box)
        {
            if (box.SelectedItems.Count < 1) return;
            Guid newId = (Guid)box.SelectedItems[box.SelectedItems.Count - 1].UserData;
            MyPluginLog.Debug("On selecet " + newId);
            m_selectedObjectId = newId;
            SetSubMenuControls();

            m_renderer.FocusObject(newId);
        }

        /// <summary>
        /// The event called when one of the zoom buttons is clicked.
        /// </summary>
        /// <param name="btn"></param>
        private void OnZoomLevelChange(MyGuiControlButton btn)
        {
            if(btn == m_zoomInButton)
            {
                m_renderer.ZoomInOnObject();
            }
            else
            {
                m_renderer.ZoomOutOfObject();
            }

            m_zoomInButton.Enabled = m_renderer.FocusZoom != ZoomLevel.OBJECT;
            m_zoomOutButton.Enabled = m_renderer.FocusZoom != ZoomLevel.ORBIT;
        }

        /// <summary>
        /// Opens window to create new System object in the system.
        /// </summary>
        /// <param name="btn"></param>
        private void AddNewSystemObject(MyGuiControlButton btn)
        {
            //Add the object to the local star system manually, sync on apply
            MyGuiScreenDialogCombobox typeDialog = new MyGuiScreenDialogCombobox("Select type", new List<string>(MySystemObjectType.GetNames(typeof(MySystemObjectType))), "The type of object to add");

            typeDialog.OnConfirm += OnTypeEntered;

            MyGuiSandbox.AddScreen(typeDialog);
        }

        /// <summary>
        /// Callback for when a type in the type dialog is confirmed.
        /// </summary>
        /// <param name="typeKey"></param>
        /// <param name="name"></param>
        private void OnTypeEntered(long typeKey, string name)
        {
            MySystemObjectType type = (MySystemObjectType)typeKey;

            if (type == MySystemObjectType.ASTEROIDS)
            {
                List<string> providers = new List<string>();
                foreach (var prov in MyAsteroidObjectsManager.Static.AsteroidObjectProviders)
                {
                    providers.Add(prov.Key);
                }

                MyGuiScreenDialogCombobox asteroidDialog = new MyGuiScreenDialogCombobox("Select Asteroid type", providers, "The asteroid type to spawn");
                asteroidDialog.OnConfirm += delegate (long key2, string typeName)
                {
                    var parent = MyStarSystemGenerator.Static.StarSystem.GetById(m_selectedObjectId);

                    MySystemAsteroids roid = new MySystemAsteroids();
                    roid.AsteroidTypeName = typeName;
                    roid.Type = type;
                    roid.DisplayName = "New Asteroid";
                    roid.ParentId = m_selectedObjectId;

                    if (parent != null)
                        roid.CenterPosition = parent.CenterPosition;

                    IMyAsteroidData data = MyAsteroidObjectsManager.Static.AsteroidObjectProviders[typeName].GetDefaultData();

                    m_pendingSystemObjects.Add(roid.Id, roid);
                    m_pendingAsteroidData.Add(roid.Id, data);

                    m_selectedObjectId = roid.Id;

                    RefreshSystemList();
                };

                MyGuiSandbox.AddScreen(asteroidDialog);
            }
            else
            {
                var parent = MyStarSystemGenerator.Static.StarSystem.GetById(m_selectedObjectId);

                MySystemObject newObj;
                switch (type)
                {
                    case MySystemObjectType.PLANET:
                    case MySystemObjectType.MOON:
                        newObj = new MySystemPlanet();
                        break;
                    default:
                        newObj = new MySystemObject();
                        break;
                }

                newObj.Type = type;
                newObj.DisplayName = "New Object";
                newObj.ParentId = m_selectedObjectId;

                if (parent != null)
                    newObj.CenterPosition = parent.CenterPosition;

                m_pendingSystemObjects.Add(newObj.Id, newObj);

                m_selectedObjectId = newObj.Id;

                RefreshSystemList();
            }
        }

        /// <summary>
        /// Applies the changes of the currently selected system object, if it has changes done to it.
        /// </summary>
        /// <param name="btn"></param>
        private void ApplyChanges(MyGuiControlButton btn)
        {

        }

        /// <summary>
        /// Action called to refresh the GUI representing the current system.
        /// </summary>
        /// <param name="btn"></param>
        private void RefreshSystem(MyGuiControlButton btn)
        {
            RefreshSystemList();

            if (m_selectedObjectId != Guid.Empty)
            {
                m_systemObjectsBox.SelectSingleItem(m_itemList[m_selectedObjectId]);
            }
        }

        /// <summary>
        /// Refresh all items in the system list to represent the current system
        /// </summary>
        private void RefreshSystemList()
        {
            MyPluginLog.Debug("Refresh");
            m_systemObjectsBox.ClearItems();
            m_renderer.ClearRenderList();
            m_itemList.Clear();

            var system = MyStarSystemGenerator.Static.StarSystem;

            if (system == null) return;//Star system is null, plugin not enabled

            system.Foreach((int depth, MySystemObject obj) =>
            {
                AddObjectToList(obj, depth);

                //Add pending childs
                foreach(var entry in m_pendingSystemObjects)
                {
                    if (system.Contains(entry.Key)) continue;

                    if(entry.Value.ParentId == obj.Id)
                    {
                        AddObjectToList(entry.Value, depth + 1);
                    }
                }
            });
        }

        /// <summary>
        /// Adds a new object to the list of system objects at a given depth and adds it to the renderer
        /// of the system.
        /// </summary>
        /// <param name="obj">Object to add</param>
        /// <param name="depth">Depth at which to add it.</param>
        private void AddObjectToList(MySystemObject obj, int depth)
        {
            var text = new StringBuilder("");
            for (int i = 0; i < depth; i++)
                text.Append("   ");

            text.Append(obj.DisplayName);
            if (m_pendingSystemObjects.ContainsKey(obj.Id))
            {
                text.Append(" *");
            }

            var item = new MyGuiControlListbox.Item(text, userData: obj.Id);
            m_systemObjectsBox.Add(item);
            m_itemList.Add(obj.Id, item);

            AddObjectToRenderer(obj);
        }

        /// <summary>
        /// Adds an object to the renderer of the system
        /// </summary>
        /// <param name="obj">Object to add</param>
        private void AddObjectToRenderer(MySystemObject obj)
        {
            MyAbstractStarSystemDesignerRenderObject render = null;

            if (obj.Type == MySystemObjectType.PLANET || obj.Type == MySystemObjectType.MOON)
            {
                m_renderer.AddObject(obj.Id, new MyPlanetOrbitRenderObject(obj as MySystemPlanet));
            }
            else if (obj.Type == MySystemObjectType.ASTEROIDS)
            {
                MySystemAsteroids roid = obj as MySystemAsteroids;
                MyAbstractAsteroidObjectProvider prov;

                if(MyAsteroidObjectsManager.Static.AsteroidObjectProviders.TryGetValue(roid.AsteroidTypeName, out prov))
                {
                    if (m_pendingAsteroidData.ContainsKey(obj.Id))
                    {
                        render = prov.GetRenderObject(roid, m_pendingAsteroidData[obj.Id]);
                    }
                    else
                    {
                        render = prov.GetRenderObject(roid, prov.GetInstanceData(roid.Id));
                    }
                }
            }
            else
            {
                m_renderer.AddObject(obj.Id, new MyEmptyObjectRenderObject(obj));
            }

            if (render != null)
            {
                m_renderer.AddObject(obj.Id, render);
            }
        }
    }
}
