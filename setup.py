from setuptools import setup, find_packages

setup(
    name='bmx',
    version='0.0.1',
    description='An AWS CLI wrapper that renews STS tokens',
    url='https://github.com/Brightspace/bmx',
    licence='Proprietary',
    classifiers = [
	'Development Status :: 3 - Alpha',

    	'Intended Audience :: Developers',
    	'Topic :: Security',

	'License :: Other/Proprietary License'
    ],
    keywords='aws cli okta sts token credentials',
    packages=find_packages(),
    entry_points={
        'console_scripts': [
            'bmx=bmx:main',
            'bmx-aws=bmxaws:main',
            'bmx-renew=bmxrenew:main'
        ]
    }
)

