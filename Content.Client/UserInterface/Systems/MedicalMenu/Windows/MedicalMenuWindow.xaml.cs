﻿using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.MedicalMenu.Windows;

[GenerateTypedNameReferences]
public sealed partial class MedicalMenuWindow : FancyWindow
{
    public MedicalMenuWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }
}