pipeline {
    agent none

    // Serialize all Jenkinsfile runs across the entire repository — only one
    // pipeline (PR, master, or any branch) executes at a time. The 14-stage
    // parallel matrix consumes a large slice of the agent pool per run; without
    // this lock, two concurrent PRs can saturate it. Queued runs wait their
    // turn rather than starve agents from each other. Lockable Resources plugin
    // provides this — the named resource doesn't need to be pre-defined; it's
    // created on first use.
    options {
        lock(resource: 'dotnetworkqueue-ci')
    }

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

                sh '''
                    dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-core/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-relational/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-sqlserver/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-postgresql/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-redis/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-sqlite/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-litedb/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-memory/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-dashboard-api/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Client.Tests/DotNetWorkQueue.Dashboard.Client.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-dashboard-client/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"

                    dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" \
                        -f net10.0 -c Debug \
                        /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/unit-dashboard-ui/ \
                        --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                '''

                stash includes: 'coverage/**/*.xml', name: 'unit-coverage'
                stash includes: 'junit-results/**/*.xml', name: 'junit-unit', allowEmpty: true
            }
        }

        stage('Integration Tests') {
            parallel {
                stage('SqlServer') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 0, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            withCredentials([string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN')]) {
                                sh 'echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/bin/Debug/net10.0/connectionstring.txt"'
                            }
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-sqlserver/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml" \
                                    -- --retry-failed-tests 1
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlserver', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-sqlserver', allowEmpty: true
                    }
                }

                stage('SqlServer Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 5, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            withCredentials([string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN')]) {
                                sh 'echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                            }
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-sqlserver-linq/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml" \
                                    -- --retry-failed-tests 1
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlserver-linq', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-sqlserver-linq', allowEmpty: true
                    }
                }

                stage('PostgreSQL') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 10, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            withCredentials([string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN')]) {
                                sh 'echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                            }
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-postgresql/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml" \
                                    -- --retry-failed-tests 1
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-postgresql', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-postgresql', allowEmpty: true
                    }
                }

                stage('PostgreSQL Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 15, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            withCredentials([string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN')]) {
                                sh 'echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                            }
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-postgresql-linq/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml" \
                                    -- --retry-failed-tests 1
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-postgresql-linq', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-postgresql-linq', allowEmpty: true
                    }
                }

                stage('Redis') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 20, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            withCredentials([string(credentialsId: 'redis-connstring', variable: 'REDIS_CONN')]) {
                                sh 'echo "$REDIS_CONN" > "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/bin/Debug/net10.0/connectionstring.txt"'
                            }
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    --filter "TestCategory!=StarvationBaseline" \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-redis/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml" \
                                    -- --retry-failed-tests 1
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-redis', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-redis', allowEmpty: true
                    }
                }

                stage('Redis Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 25, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            withCredentials([string(credentialsId: 'redis-connstring', variable: 'REDIS_CONN')]) {
                                sh 'echo "$REDIS_CONN" > "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"'
                            }
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    --filter "TestCategory!=StarvationBaseline" \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-redis-linq/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml" \
                                    -- --retry-failed-tests 1
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-redis-linq', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-redis-linq', allowEmpty: true
                    }
                }

                stage('SQLite') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 30, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-sqlite/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlite', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-sqlite', allowEmpty: true
                    }
                }

                stage('SQLite Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 35, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-sqlite-linq/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-sqlite-linq', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-sqlite-linq', allowEmpty: true
                    }
                }

                stage('LiteDB') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 40, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-litedb/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-litedb', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-litedb', allowEmpty: true
                    }
                }

                stage('LiteDB Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 45, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-litedb-linq/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-litedb-linq', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-litedb-linq', allowEmpty: true
                    }
                }

                stage('Memory') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 50, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-memory/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-memory', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-memory', allowEmpty: true
                    }
                }

                stage('Memory Linq') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 55, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-memory-linq/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-memory-linq', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-memory-linq', allowEmpty: true
                    }
                }

                stage('Dashboard') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 60, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            withCredentials([
                                string(credentialsId: 'sqlserver-connstring', variable: 'SQLSERVER_CONN'),
                                string(credentialsId: 'postgresql-connstring', variable: 'POSTGRESQL_CONN'),
                                string(credentialsId: 'redis-connstring', variable: 'REDIS_CONN')
                            ]) {
                                sh '''
                                    echo "$SQLSERVER_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring.txt"
                                    echo "$POSTGRESQL_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring-postgresql.txt"
                                    echo "$REDIS_CONN" > "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/bin/Debug/net10.0/connectionstring-redis.txt"
                                '''
                            }
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    /p:CollectCoverage=true /p:CoverletOutput=$WORKSPACE/coverage/int-dashboard/ \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'coverage/**/*.xml', name: 'cov-dashboard', allowEmpty: true
                        stash includes: 'junit-results/**/*.xml', name: 'junit-dashboard', allowEmpty: true
                    }
                }

                stage('TaskScheduler Distributed') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 65, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                            sh '''
                                dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" \
                                    -f net10.0 -c Debug \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'junit-results/**/*.xml', name: 'junit-taskscheduler', allowEmpty: true
                    }
                }

                stage('Dashboard UI E2E') {
                    // Uses the repo's standard docker-labeled agent (same as other stages)
                    // and installs Chromium + its system dependencies at stage time via
                    // Microsoft.Playwright.dll's embedded install command. The earlier
                    // approach of pulling mcr.microsoft.com/playwright/dotnet failed
                    // because the agent lacks a Docker CLI for docker-in-docker.
                    agent { label 'docker' }
                    steps {
                        sleep(time: 70, unit: 'SECONDS')
                        catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
                            sh '''
                                dotnet build "Source/DotNetWorkQueue.Dashboard.Ui.E2E.Tests/DotNetWorkQueue.Dashboard.Ui.E2E.Tests.csproj" -c Debug

                                # Install Playwright browsers (Chromium only) + apt system deps.
                                dotnet exec \
                                    --runtimeconfig Source/DotNetWorkQueue.Dashboard.Ui.E2E.Tests/bin/Debug/net10.0/DotNetWorkQueue.Dashboard.Ui.E2E.Tests.runtimeconfig.json \
                                    Source/DotNetWorkQueue.Dashboard.Ui.E2E.Tests/bin/Debug/net10.0/Microsoft.Playwright.dll \
                                    install --with-deps chromium

                                dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.E2E.Tests/DotNetWorkQueue.Dashboard.Ui.E2E.Tests.csproj" \
                                    --no-build -c Debug \
                                    --logger "junit;LogFilePath=$WORKSPACE/junit-results/{assembly}.{framework}.xml"
                            '''
                        }
                        stash includes: 'junit-results/**/*.xml', name: 'junit-e2e', allowEmpty: true
                    }
                }
            }
        }

        stage('Coverage Report') {
            agent { label 'docker' }
            steps {
                script {
                    def covStashes = [
                        'unit-coverage',
                        'cov-sqlserver', 'cov-sqlserver-linq',
                        'cov-postgresql', 'cov-postgresql-linq',
                        'cov-redis', 'cov-redis-linq',
                        'cov-sqlite', 'cov-sqlite-linq',
                        'cov-litedb', 'cov-litedb-linq',
                        'cov-memory', 'cov-memory-linq',
                        'cov-dashboard'
                    ]
                    covStashes.each { s ->
                        try { unstash s } catch (Exception e) { echo "Coverage unstash '${s}' skipped: ${e.message}" }
                    }
                }

                withCredentials([string(credentialsId: 'reportgenerator-license', variable: 'REPORTGENERATOR_LICENSE')]) {
                    sh '''
                        dotnet tool install -g dotnet-reportgenerator-globaltool || true
                        export PATH="$PATH:$HOME/.dotnet/tools"

                        reportgenerator \
                            -reports:"coverage/**/*.cobertura.xml" \
                            -targetdir:coverage/report \
                            -reporttypes:"Html;Cobertura;Badges;TeamCitySummary" \
                            -license:"$REPORTGENERATOR_LICENSE"

                        echo "Merged coverage report generated at coverage/report/"
                    '''
                }

                publishHTML(target: [
                    allowMissing: true,
                    alwaysLinkToLastBuild: true,
                    keepAll: true,
                    reportDir: 'coverage/report',
                    reportFiles: 'index.html',
                    reportName: 'Code Coverage Report'
                ])

                stash includes: 'coverage/report/Cobertura.xml', name: 'merged-cobertura', allowEmpty: true
            }
        }

        stage('Codecov Upload') {
            // Only upload coverage when the build is clean. A failed stage above
            // marks currentBuild.currentResult as FAILURE, which skips this step
            // while still letting the local HTML report (above) publish for the
            // failed build's coverage view.
            when {
                expression { currentBuild.currentResult == 'SUCCESS' }
            }
            agent { label 'docker' }
            steps {
                script {
                    try { unstash 'merged-cobertura' } catch (Exception e) { echo "merged-cobertura unstash skipped: ${e.message}" }
                }
                withCredentials([string(credentialsId: 'codecov-token', variable: 'CODECOV_TOKEN')]) {
                    sh '''
                        if [ ! -f coverage/report/Cobertura.xml ]; then
                            echo "No merged Cobertura report found; skipping codecov upload."
                            exit 0
                        fi
                        curl -Os https://cli.codecov.io/latest/linux/codecov
                        chmod +x codecov
                        ./codecov upload-process --file coverage/report/Cobertura.xml --token "$CODECOV_TOKEN" || echo "Codecov upload failed (non-fatal)"
                    '''
                }
            }
        }
    }

    post {
        always {
            // Pipeline-level post action — agent is `none`, so wrap in a node.
            // Unstash each per-stage junit bundle inside its own try/catch so an
            // early-stage failure that never produced a stash doesn't break the
            // publish for the rest. The junit step itself is tolerant of empty
            // results via allowEmptyResults.
            node('docker') {
                script {
                    def junitStashes = [
                        'junit-unit',
                        'junit-sqlserver', 'junit-sqlserver-linq',
                        'junit-postgresql', 'junit-postgresql-linq',
                        'junit-redis', 'junit-redis-linq',
                        'junit-sqlite', 'junit-sqlite-linq',
                        'junit-litedb', 'junit-litedb-linq',
                        'junit-memory', 'junit-memory-linq',
                        'junit-dashboard',
                        'junit-taskscheduler',
                        'junit-e2e'
                    ]
                    junitStashes.each { s ->
                        try { unstash s } catch (Exception e) { echo "JUnit unstash '${s}' skipped: ${e.message}" }
                    }
                }
                junit allowEmptyResults: true, testResults: 'junit-results/**/*.xml'
            }
        }
        failure {
            echo 'Pipeline failed. Check stage logs for details.'
        }
        success {
            echo 'Pipeline completed successfully.'
        }
    }
}
