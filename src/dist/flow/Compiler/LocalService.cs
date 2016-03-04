/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2015 Microsoft Corporation
 * 
 * -=- Robust Distributed System Nucleus (rDSN) -=- 
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/*
 * Description:
 *     What is this file about?
 *
 * Revision history:
 *     Feb., 2016, @imzhenyu (Zhenyu Guo), done in Tron project and copied here
 *     xxxx-xx-xx, author, fix bug about xxx
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Diagnostics;

using BondNetlibTransport;
using BondTransport;

using Microsoft.CSharp;
using rDSN.Tron.Utility;
using rDSN.Tron.Contract;
using rDSN.Tron.LanguageProvider;
using rDSN.Tron.Runtime;

namespace rDSN.Tron.Compiler
{
    public class LocalService
    {
        public LocalService(ServicePlan plan)
        {
            Name = plan.Package.Name;
            _plan = plan;
            _serviceBinary = Path.Combine(Name, Name + ".dll");

            InitMesh();

            if (null == _meta)
            {
                _metaAddress.Host = Configuration.Instance().Get<string>("MetaServer", "Host", "localhost");
                _metaAddress.Port = Configuration.Instance().Get<int>("MetaServer", "Port", 20001);
                _meta = new MetaServer_Proxy(_transport.Connect(_metaAddress.Host, _metaAddress.Port));
            }
        }

        public ErrorCode Start()
        {
            foreach (var s in _plan.DependentServices)
            {
                ErrorCode code = InitService(s);
                if (code != ErrorCode.Success && code != ErrorCode.AppServiceAlreadyExist)
                    return code;
            }

            return _mesh.Start();
        }
        
        public TResponse Call<TRequest, TResponse>(string methodName, TRequest request)
        {
            var method = _mesh.GetType().GetMethod(methodName);
            Trace.Assert(method != null, "Invoked method '" + methodName + "' not exist in '" + Name + "'");

            return ((IValue<TResponse>)method.Invoke(_mesh, new object[] { new IValue<TRequest>(request) })).Value();
        }

        // TODO: FIX ME
        public void CallDemo(string methodName, string query)
        {
            var method = _mesh.GetType().GetMethod(methodName);
            Trace.Assert(method != null, "Invoked method '" + methodName + "' not exist in '" + Name + "'");

            var req = method.GetParameters()[0].ParameterType.GetGenericArguments()[0].GetConstructor(new Type[] { }).Invoke(new object[] { });

            var prop = req.GetType().GetProperty("Query");
            prop.SetValue(req, query);

            var ireq = typeof(IValue<>).MakeGenericType(new Type[] { req.GetType() }).GetConstructor(new Type[] { req.GetType() }).Invoke(new object[] { req });

            method.Invoke(_mesh, new object[] { ireq });
        }

        private void InitMesh()
        {
            Assembly asm = Assembly.LoadFrom(_serviceBinary);
            var types = asm.ExportedTypes.Where(t => t.IsInheritedTypeOf(typeof(ServiceMesh))).ToArray();
            Trace.Assert(types.Length == 1);
            _mesh = (ServiceMesh)types[0].GetConstructor(new Type[] { }).Invoke(new object[] { });
            Trace.Assert(null != _mesh);
        }

        private ErrorCode InitService(Service svc)
        {
            ServiceInfo si = new ServiceInfo();
            si.InternalServiceSequenceId = 0;
            si.Name = svc.Name;
            si.PartitionCount = 1;
            si.ServicePackageName = svc.PackageName;

            RpcError err2 = _meta.CreateService(si);

            Trace.WriteLine("Init service '" + svc.Name + "', package = '" + svc.PackageName + "', error = " + ((ErrorCode)err2.Code).ToString());

            return (ErrorCode)(err2.Code);
        }


        public String Name { get; private set; }

        private ServiceMesh _mesh;
        private ServicePlan _plan;
        private string _serviceBinary;

        private NodeAddress _metaAddress = new NodeAddress();
        private MetaServer_Proxy _meta = null;
        private ITransport _transport = new NetlibTransport(new BinaryProtocolFactory());
    }
}