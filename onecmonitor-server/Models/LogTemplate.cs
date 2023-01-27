using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using OnecMonitor.Server.Converters.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Server.Models
{
    public class LogTemplate
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public List<TechLogSeance> Seances { get; set; } = new();

        public override bool Equals(object? obj)
        {
            return obj is LogTemplate log &&
                   Id.Equals(log.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public static Guid ServerMonitoringId => new("dc610b92-6851-4f13-8cb4-78d457cb74c1");
        public static string ServerMonitoringTemplate =>
            """
            <log location="{LOG_PATH}" history="1">
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
            """;
        public static Guid WaitingsOnManagedLocksId => new("35eb7ecf-1280-4c82-9513-2c5bfd2a7dda");
        public static string WaitingsOnManagedLocksTemplate =>
            """
            <log location="{LOG_PATH}" history="1">
                <event>
                    <eq property="name" value="SDBL"/>
                    <eq property="Func_1" value="CommitTransaction"/>
                    <eq property="Func_1" value="RollbackTransaction"/>
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
            """;
        public static Guid TimeoutsOnManagedLocksId => new("f869c902-37a2-414c-8002-c46e18362948");
        public static string TimeoutsOnManagedLocksTemplate =>
            """
            <log location="{LOG_PATH}" history="1">
                <event>
                    <eq property="name" value="SDBL"/>
                    <eq property="Func_1" value="CommitTransaction"/>
                    <eq property="Func_1" value="RollbackTransaction"/>
                </event>
                <event>
                    <eq property="name" value="TLOCK"/>
                </event>
                <event>
                    <eq property="name" value="TTIMEOUT"/>
                </event>
                <property name="all"/>
            </log>
            """;
        public static Guid DeadlocksOnManagedLocksId => new("d4f5551a-995a-480c-8487-e30d96605c1a");
        public static string CallsScallTemplate =>
            """
            <log location="{LOG_PATH}" history="1">
                <event>
                    <eq property="name" value="SCALL"/>
                </event>
                <event>
                    <eq property="name" value="CALL"/>
                </event>
                <property name="all"/>
            </log>
            """;
        public static Guid CallScallsId => new("f20908d6-2c2c-4a7b-82e4-4c62b6a8f99a");
        public static string DeadlocksOnManagedLocksTemplate =>
            """
            <log location="{LOG_PATH}" history="1">
                <event>
                    <eq property="name" value="SDBL"/>
                    <eq property="Func_1" value="CommitTransaction"/>
                    <eq property="Func_1" value="RollbackTransaction"/>
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
            """;
        public static Guid FullId => new("54695318-0ca1-4c95-896f-731872fb1c0e");
        public static string FullTemplate =>
            """
            <log location="{LOG_PATH}" history="4">
                <event>
                    <ne property="name" value=""/>
                </event>
                <property name="all"/>
            </log>
            """;
        public static Guid[] BuiltInTemplatesIds => new[]
        {
            ServerMonitoringId,
            WaitingsOnManagedLocksId,
            TimeoutsOnManagedLocksId,
            DeadlocksOnManagedLocksId,
            CallScallsId,
            FullId
        };
    }
}
