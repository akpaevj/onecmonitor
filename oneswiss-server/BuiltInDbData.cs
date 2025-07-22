using System.Collections.ObjectModel;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.Server.Models.MaintenanceTasks;

namespace OneSwiss.Server;

public static class BuiltInDbData
{
    public static List<(string Name, string Id, string Content)> LogTemplates { get; } =
    [
        (
            "Мониторинг",
            "796d8c2a-4b7b-4d3c-bc54-92a66af56db0",
            """
            <log history="1">
                <event>
                    <eq property="Name" value="EXCP"/>
                </event>
                <event>
                    <eq property="Name" value="CONN"/>
                </event>
                <event>
                    <eq property="Name" value="PROC"/>
                </event>
                <event>
                    <eq property="Name" value="ADMIN"/>
                </event>
                <event>
                    <eq property="Name" value="SESN"/>
                </event>
                <event>
                    <eq property="Name" value="CLSTR"/>
                </event>
                <property name="all"/>
            </log>
            """
        ),

        (
            "Взаимоблокировки на управляемых блокировках",
            "8d85a97e-9aa3-4d2a-b723-ef0c57444de3",
            """
            <log history="1">
                <event>
                    <eq property="name" value="SDBL"/>
                    <eq property="Func1" value="CommitTransaction"/>
                    <eq property="Func1" value="RollbackTransaction"/>
                </event>
                <event>
                    <eq property="name" value="TLOCK"/>
                </event>
                <event>
                    <eq property="name" value="TTIMEOUT"/>
                </event>
                <event>
                    <eq property="name" value="TDEADLOCK"/>
                </event>
                <property name="all"/>
            </log>
            """
        ),

        (
            "Таймауты на управляемых блокировках",
            "93ebb31d-b686-4d50-9549-281b09f6fad4",
            """
            <log history="1">
                <event>
                    <eq property="name" value="SDBL"/>
                    <eq property="Func1" value="CommitTransaction"/>
                    <eq property="Func1" value="RollbackTransaction"/>
                </event>
                <event>
                    <eq property="name" value="TLOCK"/>
                </event>
                <event>
                    <eq property="name" value="TTIMEOUT"/>
                </event>
                <property name="all"/>
            </log>
            """
        ),

        (
            "CALL и SCALL",
            "834e5bc5-fff2-45a8-9ec1-e4d00f547514",
            """
            <log history="1">
                <event>
                    <eq property="name" value="SCALL"/>
                </event>
                <event>
                    <eq property="name" value="CALL"/>
                </event>
                <property name="all"/>
            </log>
            """
        ),

        (
            "Ожидания на управляемых блокировках",
            "95094c0b-4cb5-45e6-88af-3f23a28528cf",
            """
            <log history="1">
                <event>
                    <eq property="name" value="SDBL"/>
                    <eq property="Func1" value="CommitTransaction"/>
                    <eq property="Func1" value="RollbackTransaction"/>
                </event>
                <event>
                    <eq property="name" value="TLOCK"/>
                </event>
                <event>
                    <eq property="name" value="TTIMEOUT"/>
                </event>
                <event>
                    <eq property="name" value="TDEADLOCK"/>
                </event>
                <property name="all"/>
            </log>
            """
        ),

        (
            "Полный",
            "a15c2118-8035-42bd-b37e-edae90a2b839",
            """
            <log history="4">
                <event>
                    <ne property="name" value=""/>
                </event>
                <property name="all"/>
            </log>
            """
        ),

        (
            "VRSREQUEST и VRSRESPONSE",
            "43a87481-bcd2-45e5-816b-b3a40c45f2f7",
            """
            <log history="4">
                <event>
                    <eq property="name" value="VRSREQUEST"/>
                </event>
                <event>
                    <eq property="name" value="VRSRESPONSE"/>
                </event>
                <property name="all"/>
            </log>
            """
        )
    ];
}