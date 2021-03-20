﻿// -----------------------------------------------------------------------
//  <copyright file="EntityFrameworkCorePack.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2018 OSharp. All rights reserved.
//  </copyright>
//  <site>http://www.osharp.org</site>
//  <last-editor>郭明锋</last-editor>
//  <last-date>2018-12-14 15:57</last-date>
// -----------------------------------------------------------------------

using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using OSharp.Authorization.EntityInfos;
using OSharp.Collections;
using OSharp.Core.Options;
using OSharp.Core.Packs;
using OSharp.Data.Snows;
using OSharp.Entity.KeyGenerate;
using OSharp.EventBuses;
using OSharp.Exceptions;


namespace OSharp.Entity
{
    /// <summary>
    /// EntityFrameworkCore基模块
    /// </summary>
    [DependsOnPacks(typeof(EventBusPack), typeof(EntityInfoPack))]
    public abstract class EntityFrameworkCorePackBase : OsharpPack
    {
        /// <summary>
        /// 获取 模块级别，级别越小越先启动
        /// </summary>
        public override PackLevel Level => PackLevel.Framework;

        /// <summary>
        /// 将模块服务添加到依赖注入服务容器中
        /// </summary>
        /// <param name="services">依赖注入服务容器</param>
        /// <returns></returns>
        public override IServiceCollection AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<IKeyGenerator<int>, AutoIncreaseKeyGenerator>();
            services.TryAddSingleton<IKeyGenerator<long>>(new SnowKeyGenerator(new DefaultIdGenerator(new IdGeneratorOptions(1))));

            services.TryAddScoped<IAuditEntityProvider, AuditEntityProvider>();
            services.TryAddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            services.TryAddScoped<IUnitOfWork, UnitOfWork>();
            services.TryAddSingleton<IEntityManager, EntityManager>();
            services.AddSingleton<DbContextModelCache>();
            services.AddSingleton<IEntityBatchConfiguration, TableNamePrefixConfiguration>();
            services.AddOsharpDbContext<DefaultDbContext>();

            return services;
        }

        /// <summary>
        /// 应用模块服务
        /// </summary>
        /// <param name="provider">服务提供者</param>
        public override void UsePack(IServiceProvider provider)
        {
            var dbContextOptions = provider.GetOSharpOptions().DbContexts;
            if (dbContextOptions.IsNullOrEmpty())
            {
                throw new OsharpException("配置文件中找不到数据上下文的配置，请配置OSharp:DbContexts节点");
            }

            foreach (var options in dbContextOptions)
            {
                string msg = options.Value.Error;
                if (msg != null)
                {
                    throw new OsharpException($"数据库“{options.Key}”配置错误：{msg}");
                }
            }

            IEntityManager manager = provider.GetRequiredService<IEntityManager>();
            manager.Initialize();
            IsEnabled = true;
        }
    }
}