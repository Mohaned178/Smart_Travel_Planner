using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTravelPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDomainModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "MealTime",
                table: "RestaurantSuggestions",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportMode",
                table: "ActivitySlots",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealTime",
                table: "RestaurantSuggestions");

            migrationBuilder.DropColumn(
                name: "TransportMode",
                table: "ActivitySlots");
        }
    }
}
