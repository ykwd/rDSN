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
 *     a simple uri resolver that queries meta server
 *
 * Revision history:
 *     Feb., 2016, @imzhenyu (Zhenyu Guo), first draft
 *     xxxx-xx-xx, author, fix bug about xxx
 */

# pragma once

# include <dsn/dist/partition_resolver.h>
# include <dsn/cpp/zlocks.h>

namespace dsn
{
    namespace dist
    {
#pragma pack(push, 4)
        class partition_resolver_simple
            : public partition_resolver,
              public virtual clientlet
        {
        public:
            partition_resolver_simple(
                rpc_address meta_server, 
                const char* app_path);

            virtual void resolve(
                uint64_t partition_hash,
                std::function<void(dist::partition_resolver::resolve_result&&)>&& callback,
                int timeout_ms
                ) override;

            virtual void on_access_failure(int partition_index, error_code err) override;

            virtual int get_partition_index(int partition_count, uint64_t partition_hash) override;

            ~partition_resolver_simple();

            int get_partition_count() const { return _app_partition_count; }

        private:
            mutable dsn::service::zrwlock_nr     _config_lock;
            std::unordered_map<int, ::dsn::partition_configuration> _config_cache;
            int                                  _app_id;
            int                                  _app_partition_count;
            bool                                 _app_is_stateful;

        private:
            typedef std::function<void(dist::partition_resolver::resolve_result&&)> callback_t;
            struct request_context : public ref_counter, public transient_object
            {
                int                   partition_index;
                uint64_t              partition_hash;
                callback_t            callback;
                int                   timeout_ms; // init timeout
                uint64_t              timeout_ts_us; // timeout at this timing point

                ::dsn::service::zlock lock; // [
                dsn::task_ptr         timeout_timer; // when partition config is unknown at the first place
                bool                  completed;
                // ]
            };
            typedef ::dsn::ref_ptr<request_context> request_context_ptr;

            struct partition_context
            {
                dsn::task_ptr     query_config_task;
                std::list<request_context_ptr> requests;
            };

            typedef std::unordered_map<int, partition_context*> pending_replica_requests;

            mutable dsn::service::zlock     _requests_lock;
            pending_replica_requests        _pending_requests;
            std::list<request_context_ptr>  _pending_requests_before_partition_count_unknown;
            task_ptr                        _query_config_task;

        private:
            // local routines
            dsn::rpc_address get_address(const ::dsn::partition_configuration& config);
            error_code get_address(int partition_index, /*out*/ dsn::rpc_address& addr);
            void handle_pending_requests(std::list<request_context_ptr>& reqs, error_code err);
            void clear_all_pending_requests();

            // with replica
            void call(request_context_ptr&& request, bool from_meta_ack = false);
            //void replica_rw_reply(error_code err, dsn_message_t request, dsn_message_t response, request_context_ptr rc);
            void end_request(request_context_ptr&& request, error_code err, rpc_address addr);
            void on_timeout(request_context_ptr&& rc);

            // with meta server
            dsn::task_ptr query_partition_config(int partition_index);
            void query_partition_configuration_reply(error_code err, dsn_message_t request, dsn_message_t response, int partition_index);
        };
#pragma pack(pop)
    }
}
