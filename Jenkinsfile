pipeline {
    agent none

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO = '1'
        NUGET_XMLDOC_MODE = 'skip'
    }

    stages {
        stage('Build & Unit Tests') {
            agent { label 'docker' }
            steps {
                sh 'dotnet restore "Source/DotNetWorkQueue.sln"'
                sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug --no-restore'

                // Run all unit tests with coverage
                sh '''
                    dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-core

                    dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-relational

                    dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-sqlserver

                    dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-postgresql

                    dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-redis

                    dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-sqlite

                    dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-litedb

                    dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-memory

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-dashboard-api

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Client.Tests/DotNetWorkQueue.Dashboard.Client.Tests.csproj" \
                        -f net10.0 --no-build -c Debug \
                        --collect:"XPlat Code Coverage" --results-directory coverage/unit-dashboard-client
                '''

                stash includes: 'coverage/**/*.xml', name: 'unit-coverage'
            }
        }

        stage('Integration Tests') {
            parallel {
                // Agent 1: SqlServer Linq + Redis (~58 min)
                stage('SqlServerLinq + Redis') {
                    agent { label 'docker' }
                    steps {
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'

                        withCredentials([string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN')]) {
                            sh '''
                                echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"
                            '''
                        }

                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlserver-linq

                            dotnet test "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-redis
                        '''

                        stash includes: 'coverage/**/*.xml', name: 'coverage-agent1'
                    }
                }

                // Agent 2: SqlServer + Dashboard (~54 min)
                stage('SqlServer + Dashboard') {
                    agent { label 'docker' }
                    steps {
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'

                        withCredentials([
                            string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN'),
                            string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN')
                        ]) {
                            sh '''
                                echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/bin/Debug/net10.0/connectionstring.txt"
                                echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"
                                echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring-postgresql.txt"
                                echo "192.168.0.2,defaultDatabase=1,syncTimeout=15000" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring-redis.txt"
                            '''
                        }

                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlserver

                            dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-dashboard
                        '''

                        stash includes: 'coverage/**/*.xml', name: 'coverage-agent2'
                    }
                }

                // Agent 3: SQLite Linq + Memory (~51 min)
                stage('SQLiteLinq + Memory') {
                    agent { label 'docker' }
                    steps {
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'

                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlite-linq

                            dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-memory
                        '''

                        stash includes: 'coverage/**/*.xml', name: 'coverage-agent3'
                    }
                }

                // Agent 4: PostgreSQL + Memory Linq (~54 min)
                stage('PostgreSQL + MemoryLinq') {
                    agent { label 'docker' }
                    steps {
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'

                        withCredentials([string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN')]) {
                            sh '''
                                echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"
                            '''
                        }

                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-postgresql

                            dotnet test "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-memory-linq
                        '''

                        stash includes: 'coverage/**/*.xml', name: 'coverage-agent4'
                    }
                }

                // Agent 5: PostgreSQL Linq + LiteDB (~56 min)
                stage('PostgreSQLLinq + LiteDB') {
                    agent { label 'docker' }
                    steps {
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'

                        withCredentials([string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN')]) {
                            sh '''
                                echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"
                            '''
                        }

                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-postgresql-linq

                            dotnet test "Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-litedb
                        '''

                        stash includes: 'coverage/**/*.xml', name: 'coverage-agent5'
                    }
                }

                // Agent 6: SQLite + LiteDB Linq + Redis Linq (~63 min)
                stage('SQLite + LiteDBLinq + RedisLinq') {
                    agent { label 'docker' }
                    steps {
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'

                        sh '''
                            dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-sqlite

                            dotnet test "Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-litedb-linq

                            dotnet test "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj" \
                                -f net10.0 -c Debug \
                                --filter "FullyQualifiedName!~JobScheduler" \
                                --collect:"XPlat Code Coverage" --results-directory coverage/int-redis-linq
                        '''

                        stash includes: 'coverage/**/*.xml', name: 'coverage-agent6'
                    }
                }
            }
        }

        stage('Coverage Report') {
            agent { label 'docker' }
            steps {
                // Collect all coverage files
                unstash 'unit-coverage'
                unstash 'coverage-agent1'
                unstash 'coverage-agent2'
                unstash 'coverage-agent3'
                unstash 'coverage-agent4'
                unstash 'coverage-agent5'
                unstash 'coverage-agent6'

                // Install ReportGenerator and merge coverage
                sh '''
                    dotnet tool install -g dotnet-reportgenerator-globaltool || true
                    export PATH="$PATH:$HOME/.dotnet/tools"

                    reportgenerator \
                        -reports:"coverage/**/coverage.cobertura.xml" \
                        -targetdir:coverage/report \
                        -reporttypes:"HtmlInline_AzurePipelines;Cobertura"

                    echo "Merged coverage report generated at coverage/report/"
                '''

                // Upload to Codecov
                withCredentials([string(credentialsId: 'codecov-token', variable: 'CODECOV_TOKEN')]) {
                    sh '''
                        curl -Os https://cli.codecov.io/latest/linux/codecov
                        chmod +x codecov
                        ./codecov --file coverage/report/Cobertura.xml --token "$CODECOV_TOKEN" || echo "Codecov upload failed (non-fatal)"
                    '''
                }

                // Archive HTML report
                publishHTML(target: [
                    allowMissing: false,
                    alwaysLinkToLastBuild: true,
                    keepAll: true,
                    reportDir: 'coverage/report',
                    reportFiles: 'index.html',
                    reportName: 'Code Coverage Report'
                ])
            }
        }
    }

    post {
        failure {
            echo 'Pipeline failed. Check stage logs for details.'
        }
        success {
            echo 'Pipeline completed successfully.'
        }
    }
}
