﻿using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Domains;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Mapping.Builders;
public class PluginBuilder : NopEntityBuilder<CustomTable>
{
    #region Methods

    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
    }

    #endregion
}