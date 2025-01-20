using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrainStormEra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievement",
                columns: table => new
                {
                    achievement_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    achievement_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    achievement_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    achievement_icon = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    achievement_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__achievem__3C492E839079A79E", x => x.achievement_id);
                });

            migrationBuilder.CreateTable(
                name: "course_category",
                columns: table => new
                {
                    course_category_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    course_category_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__course_c__FE7D58E869A5A9CB", x => x.course_category_id);
                });

            migrationBuilder.CreateTable(
                name: "lesson_type",
                columns: table => new
                {
                    lesson_type_id = table.Column<int>(type: "int", nullable: false),
                    lesson_type_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__lesson_t__F5960D1E4342B14D", x => x.lesson_type_id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    user_role = table.Column<int>(type: "int", nullable: false),
                    role_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__role__68057FECA74D198B", x => x.user_role);
                });

            migrationBuilder.CreateTable(
                name: "status",
                columns: table => new
                {
                    status_id = table.Column<int>(type: "int", nullable: false),
                    status_description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__status__3683B5315977C7C3", x => x.status_id);
                });

            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    user_role = table.Column<int>(type: "int", nullable: true),
                    username = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    user_email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_point = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: true),
                    phone_number = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    user_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_picture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    account_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__account__B9BE370FC339DD57", x => x.user_id);
                    table.ForeignKey(
                        name: "FK__account__user_ro__3E52440B",
                        column: x => x.user_role,
                        principalTable: "role",
                        principalColumn: "user_role",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "chatbot_conversation",
                columns: table => new
                {
                    conversation_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    conversation_time = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    conversation_content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chatbot___311E7E9A786337B4", x => x.conversation_id);
                    table.ForeignKey(
                        name: "FK__chatbot_c__user___6477ECF3",
                        column: x => x.user_id,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course",
                columns: table => new
                {
                    course_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    course_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    course_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    course_status = table.Column<int>(type: "int", nullable: true),
                    course_picture = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    course_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    created_by = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__course__8F1EF7AE501E9846", x => x.course_id);
                    table.ForeignKey(
                        name: "FK__course__course_s__44FF419A",
                        column: x => x.course_status,
                        principalTable: "status",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__course__created___45F365D3",
                        column: x => x.created_by,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    payment_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    payment_description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    points_earned = table.Column<int>(type: "int", nullable: true),
                    payment_status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    payment_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__payment__ED1FC9EA3AEE907F", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK__payment__user_id__7E37BEF6",
                        column: x => x.user_id,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chapter",
                columns: table => new
                {
                    chapter_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    course_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    chapter_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    chapter_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    chapter_order = table.Column<int>(type: "int", nullable: true),
                    chapter_status = table.Column<int>(type: "int", nullable: true),
                    chapter_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chapter__745EFE87FE7B2D3A", x => x.chapter_id);
                    table.ForeignKey(
                        name: "FK__chapter__chapter__5812160E",
                        column: x => x.chapter_status,
                        principalTable: "status",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__chapter__course___571DF1D5",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course_category_mapping",
                columns: table => new
                {
                    course_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    course_category_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__course_c__10F92220DD4A169A", x => new { x.course_id, x.course_category_id });
                    table.ForeignKey(
                        name: "FK__course_ca__cours__4AB81AF0",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__course_ca__cours__4BAC3F29",
                        column: x => x.course_category_id,
                        principalTable: "course_category",
                        principalColumn: "course_category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enrollment",
                columns: table => new
                {
                    enrollment_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    course_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    enrollment_status = table.Column<int>(type: "int", nullable: true),
                    approved = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    certificate_issued_date = table.Column<DateOnly>(type: "date", nullable: true),
                    enrollment_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__enrollme__6D24AA7AF40D0BE8", x => x.enrollment_id);
                    table.ForeignKey(
                        name: "FK__enrollmen__cours__5165187F",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__enrollmen__enrol__52593CB8",
                        column: x => x.enrollment_status,
                        principalTable: "status",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__enrollmen__user___5070F446",
                        column: x => x.user_id,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    feedback_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    course_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    star_rating = table.Column<byte>(type: "tinyint", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    feedback_date = table.Column<DateOnly>(type: "date", nullable: false),
                    hidden_status = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    feedback_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__feedback__7A6B2B8CCEB7225D", x => x.feedback_id);
                    table.ForeignKey(
                        name: "FK__feedback__course__6A30C649",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__feedback__user_i__6B24EA82",
                        column: x => x.user_id,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    notification_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    course_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    notification_title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    notification_content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notification_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    notification_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    created_by = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__notifica__E059842F66E8F682", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK__notificat__cours__787EE5A0",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__notificat__creat__797309D9",
                        column: x => x.created_by,
                        principalTable: "account",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK__notificat__user___778AC167",
                        column: x => x.user_id,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson",
                columns: table => new
                {
                    lesson_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    chapter_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    lesson_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    lesson_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lesson_content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lesson_order = table.Column<int>(type: "int", nullable: false),
                    lesson_type_id = table.Column<int>(type: "int", nullable: true),
                    lesson_status = table.Column<int>(type: "int", nullable: true),
                    lesson_created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__lesson__6421F7BE17A74C7B", x => x.lesson_id);
                    table.ForeignKey(
                        name: "FK__lesson__chapter___5EBF139D",
                        column: x => x.chapter_id,
                        principalTable: "chapter",
                        principalColumn: "chapter_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__lesson__lesson_s__60A75C0F",
                        column: x => x.lesson_status,
                        principalTable: "status",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__lesson__lesson_t__5FB337D6",
                        column: x => x.lesson_type_id,
                        principalTable: "lesson_type",
                        principalColumn: "lesson_type_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_achievement",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    achievement_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: false),
                    enrollment_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_ach__9A7AA5E7DB845F37", x => new { x.user_id, x.achievement_id });
                    table.ForeignKey(
                        name: "FK__user_achi__achie__71D1E811",
                        column: x => x.achievement_id,
                        principalTable: "achievement",
                        principalColumn: "achievement_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__user_achi__enrol__72C60C4A",
                        column: x => x.enrollment_id,
                        principalTable: "enrollment",
                        principalColumn: "enrollment_id");
                    table.ForeignKey(
                        name: "FK__user_achi__user___70DDC3D8",
                        column: x => x.user_id,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_completion",
                columns: table => new
                {
                    completion_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    lesson_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    completion_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__lesson_c__FE426E9843DB08EC", x => x.completion_id);
                    table.ForeignKey(
                        name: "FK__lesson_co__lesso__02FC7413",
                        column: x => x.lesson_id,
                        principalTable: "lesson",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__lesson_co__user___02084FDA",
                        column: x => x.user_id,
                        principalTable: "account",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_user_role",
                table: "account",
                column: "user_role");

            migrationBuilder.CreateIndex(
                name: "UQ__account__B0FBA212B4233AAB",
                table: "account",
                column: "user_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__account__F3DBC572B907856D",
                table: "account",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chapter_chapter_status",
                table: "chapter",
                column: "chapter_status");

            migrationBuilder.CreateIndex(
                name: "unique_chapter_order_per_course",
                table: "chapter",
                columns: new[] { "course_id", "chapter_order" },
                unique: true,
                filter: "[course_id] IS NOT NULL AND [chapter_order] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_chatbot_conversation_user_id",
                table: "chatbot_conversation",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_course_status",
                table: "course",
                column: "course_status");

            migrationBuilder.CreateIndex(
                name: "IX_course_created_by",
                table: "course",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_course_category_mapping_course_category_id",
                table: "course_category_mapping",
                column: "course_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_course_id",
                table: "enrollment",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_enrollment_status",
                table: "enrollment",
                column: "enrollment_status");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_user_id",
                table: "enrollment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_course_id",
                table: "feedback",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_user_id",
                table: "feedback",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_lesson_status",
                table: "lesson",
                column: "lesson_status");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_lesson_type_id",
                table: "lesson",
                column: "lesson_type_id");

            migrationBuilder.CreateIndex(
                name: "unique_lesson_order_per_chapter",
                table: "lesson",
                columns: new[] { "chapter_id", "lesson_order" },
                unique: true,
                filter: "[chapter_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_completion_lesson_id",
                table: "lesson_completion",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_completion_user_id",
                table: "lesson_completion",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_course_id",
                table: "notification",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_created_by",
                table: "notification",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_notification_user_id",
                table: "notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_user_id",
                table: "payment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievement_achievement_id",
                table: "user_achievement",
                column: "achievement_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievement_enrollment_id",
                table: "user_achievement",
                column: "enrollment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chatbot_conversation");

            migrationBuilder.DropTable(
                name: "course_category_mapping");

            migrationBuilder.DropTable(
                name: "feedback");

            migrationBuilder.DropTable(
                name: "lesson_completion");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "user_achievement");

            migrationBuilder.DropTable(
                name: "course_category");

            migrationBuilder.DropTable(
                name: "lesson");

            migrationBuilder.DropTable(
                name: "achievement");

            migrationBuilder.DropTable(
                name: "enrollment");

            migrationBuilder.DropTable(
                name: "chapter");

            migrationBuilder.DropTable(
                name: "lesson_type");

            migrationBuilder.DropTable(
                name: "course");

            migrationBuilder.DropTable(
                name: "status");

            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "role");
        }
    }
}
