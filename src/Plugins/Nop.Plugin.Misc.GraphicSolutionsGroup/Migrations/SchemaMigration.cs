using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Domains;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Migrations;
[NopMigration("10/27/2024 8:11:42 PM", "Nop.Plugin.Misc.GraphicSolutionsGroup schema", MigrationProcessType.Installation)]
public class SchemaMigration : AutoReversingMigration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        Create.TableFor<CustomTable>();
    }
}